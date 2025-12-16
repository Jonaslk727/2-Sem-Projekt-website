using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Threading.Tasks;

using _2._Sem_Project_Eksamen_System.EFservices;
using _2._Sem_Project_Eksamen_System.Models1;

namespace YourTestProjectName
{
    [TestClass]
    public class EFOverlapServiceTests
    {
        private EksamensDBContext _context = null!;
        private EFOverlapService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<EksamensDBContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                // Ikke strengt nødvendigt her, men nice hvis jeres context globalt laver warnings->exceptions
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new EksamensDBContext(options);
            _service = new EFOverlapService(_context);
        }

        private async Task<Class> CreateClassAsync(string className = "Data-RO-F-V25B-2sem")
        {
            var c = new Class { ClassName = className };
            _context.Classes.Add(c);
            await _context.SaveChangesAsync();
            return c;
        }

        private async Task<Exam> CreateExamAsync(
            int classId,
            string name,
            DateOnly start,
            DateOnly end,
            bool isFinal = false,
            bool isReExam = false)
        {
            var e = new Exam
            {
                ExamName = name,
                ClassId = classId,
                ExamStartDate = start,
                ExamEndDate = end,
                IsFinalExam = isFinal,
                IsReExam = isReExam
            };
            _context.Exams.Add(e);
            await _context.SaveChangesAsync();
            return e;
        }

        // -------------------------
        // TEACHER OVERLAP
        // -------------------------

        [TestMethod]
        public async Task TeacherHasOverlap_ReturnsConflict_WhenDatesOverlap_AndNeitherAllowsOverlap()
        {
            // Arrange
            var cls = await CreateClassAsync();
            var existingExam = await CreateExamAsync(
                cls.ClassId, "ExistingExam",
                new DateOnly(2025, 1, 10),
                new DateOnly(2025, 1, 12),
                isFinal: false,
                isReExam: false);

            var teacher = new Teacher { TeacherName = "Test Teacher", Email = "t@x.dk" };
            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();

            // Link teacher <-> exam
            _context.TeachersToExams.Add(new TeachersToExam
            {
                TeacherId = teacher.TeacherId,
                ExamId = existingExam.ExamId
            });
            await _context.SaveChangesAsync();

            // Act (new exam overlaps, and new is not final/reexam)
            var result = _service.TeacherHasOverlap(
                teacher.TeacherId,
                new DateOnly(2025, 1, 11),
                new DateOnly(2025, 1, 13),
                newIsFinal: false,
                newIsReExam: false);

            // Assert
            Assert.IsTrue(result.HasConflict);
            Assert.AreEqual("Exam", result.ConflictingEntityType);
            Assert.AreEqual(existingExam.ExamId, result.ConflictingEntityId);
        }

        [TestMethod]
        public async Task TeacherHasOverlap_ReturnsOk_WhenExistingIsFinal_AllowsOverlap()
        {
            // Arrange
            var cls = await CreateClassAsync();
            var existingExam = await CreateExamAsync(
                cls.ClassId, "FinalExam",
                new DateOnly(2025, 2, 1),
                new DateOnly(2025, 2, 2),
                isFinal: true,
                isReExam: false);

            var teacher = new Teacher { TeacherName = "T", Email = "t@x.dk" };
            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();

            _context.TeachersToExams.Add(new TeachersToExam
            {
                TeacherId = teacher.TeacherId,
                ExamId = existingExam.ExamId
            });
            await _context.SaveChangesAsync();

            // Act (dates overlap, men existing er Final => overlap er OK ifølge service-reglen)
            var result = _service.TeacherHasOverlap(
                teacher.TeacherId,
                new DateOnly(2025, 2, 2),
                new DateOnly(2025, 2, 3),
                newIsFinal: false,
                newIsReExam: false);

            // Assert
            Assert.IsFalse(result.HasConflict);
        }

        [TestMethod]
        public async Task TeacherHasOverlap_IgnoresSameExam_WhenExcludeExamIdIsUsed()
        {
            // Arrange
            var cls = await CreateClassAsync();
            var existingExam = await CreateExamAsync(
                cls.ClassId, "ExamToUpdate",
                new DateOnly(2025, 3, 10),
                new DateOnly(2025, 3, 12));

            var teacher = new Teacher { TeacherName = "T", Email = "t@x.dk" };
            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();

            _context.TeachersToExams.Add(new TeachersToExam
            {
                TeacherId = teacher.TeacherId,
                ExamId = existingExam.ExamId
            });
            await _context.SaveChangesAsync();

            // Act: samme dato-range, men vi ekskluderer samme examId (update-scenarie)
            var result = _service.TeacherHasOverlap(
                teacher.TeacherId,
                new DateOnly(2025, 3, 10),
                new DateOnly(2025, 3, 12),
                newIsFinal: false,
                newIsReExam: false,
                excludeExamId: existingExam.ExamId);

            // Assert
            Assert.IsFalse(result.HasConflict);
        }

        // -------------------------
        // CLASS OVERLAP
        // -------------------------

        [TestMethod]
        public async Task ClassHasOverlap_ReturnsConflict_WhenDatesOverlap()
        {
            // Arrange
            var cls = await CreateClassAsync();
            var existingExam = await CreateExamAsync(
                cls.ClassId, "Existing",
                new DateOnly(2025, 4, 1),
                new DateOnly(2025, 4, 3));

            // Act
            var result = _service.ClassHasOverlap(
                cls.ClassId,
                new DateOnly(2025, 4, 2),
                new DateOnly(2025, 4, 4));

            // Assert
            Assert.IsTrue(result.HasConflict);
            Assert.AreEqual(existingExam.ExamId, result.ConflictingEntityId);
        }

        [TestMethod]
        public void ClassHasOverlap_Throws_WhenClassIdIsInvalid()
        {
            // Act + Assert
            Assert.ThrowsException<AggregateException>(() =>
                _service.ClassHasOverlap(
                    0,
                    new DateOnly(2025, 1, 1),
                    new DateOnly(2025, 1, 2)));
        }

        // -------------------------
        // ROOM OVERLAP
        // -------------------------

        [TestMethod]
        public async Task RoomHasOverlap_ReturnsConflict_WhenDatesOverlap()
        {
            // Arrange
            var cls = await CreateClassAsync();
            var existingExam = await CreateExamAsync(
                cls.ClassId, "RoomExam",
                new DateOnly(2025, 5, 10),
                new DateOnly(2025, 5, 12));

            var room = new Room { Name = "A101", Capacity = 30 };
            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            // Link room <-> exam
            _context.RoomsToExams.Add(new RoomsToExam
            {
                RoomId = room.RoomId,
                ExamId = existingExam.ExamId
            });
            await _context.SaveChangesAsync();

            // Act
            var result = _service.RoomHasOverlap(
                room.RoomId,
                new DateOnly(2025, 5, 11),
                new DateOnly(2025, 5, 13));

            // Assert
            Assert.IsTrue(result.HasConflict);
            Assert.AreEqual(existingExam.ExamId, result.ConflictingEntityId);
            Assert.AreEqual(room.RoomId, result.ConflictingRoomId);
        }

        [TestMethod]
        public async Task RoomHasOverlap_ReturnsOk_WhenExcludeExamIdIsUsed()
        {
            // Arrange
            var cls = await CreateClassAsync();
            var existingExam = await CreateExamAsync(
                cls.ClassId, "ExamToUpdate",
                new DateOnly(2025, 6, 1),
                new DateOnly(2025, 6, 2));

            var room = new Room { Name = "B202", Capacity = 40 };
            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            _context.RoomsToExams.Add(new RoomsToExam
            {
                RoomId = room.RoomId,
                ExamId = existingExam.ExamId
            });
            await _context.SaveChangesAsync();

            // Act: overlap, men ekskluderer samme exam (update)
            var result = _service.RoomHasOverlap(
                room.RoomId,
                new DateOnly(2025, 6, 1),
                new DateOnly(2025, 6, 2),
                excludeExamId: existingExam.ExamId);

            // Assert
            Assert.IsFalse(result.HasConflict);
        }
    }
}
