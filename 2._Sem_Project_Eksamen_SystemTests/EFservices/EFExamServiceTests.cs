using System;
using System.Linq;
using System.Threading.Tasks;
using _2._Sem_Project_Eksamen_System.EFservices;
using _2._Sem_Project_Eksamen_System.Models1;
using _2._Sem_Project_Eksamen_System.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace YourTestProjectName
{
    [TestClass]
    public class EFExamServiceTests
    {
        private EksamensDBContext _context = null!;
        private EFExamService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<EksamensDBContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new EksamensDBContext(options);
            _service = new EFExamService(_context);
        }

        private async Task<Class> CreateClassAsync(string className = "Data-RO-F-V25B-2sem")
        {
            var c = new Class { ClassName = className };
            _context.Classes.Add(c);
            await _context.SaveChangesAsync();
            return c;
        }

        [TestMethod]
        public async Task Exam_CanBeAdded_AndDeleted()
        {
            // Arrange
            var cls = await CreateClassAsync();

            var exam = new Exam
            {
                ExamName = "PROGTEK1",
                ClassId = cls.ClassId,
                ExamPatrol = false,
                IsReExam = false,
                IsFinalExam = true
            };

            // Act - Add
            await _service.AddItemAsync(exam);

            // Assert - Added
            var allAfterAdd = (await _service.GetAllAsync()).ToList();
            Assert.AreEqual(1, allAfterAdd.Count);
            Assert.AreEqual("PROGTEK1", allAfterAdd[0].ExamName);

            var savedId = allAfterAdd[0].ExamId;

            // Act - Delete
            await _service.DeleteItemAsync(savedId);

            // Assert - Deleted
            var allAfterDelete = (await _service.GetAllAsync()).ToList();
            Assert.AreEqual(0, allAfterDelete.Count);
        }

        [TestMethod]
        public async Task Exam_CanBeUpdated()
        {
            // Arrange
            var cls = await CreateClassAsync();

            var exam = new Exam
            {
                ExamName = "OldName",
                ClassId = cls.ClassId,
                ExamPatrol = false,
                IsReExam = false,
                IsFinalExam = false,
                NumOfStud = 10,
                Description = "Old desc"
            };

            await _service.AddItemAsync(exam);
            var saved = (await _service.GetAllAsync()).First();

            // Act
            var updated = new Exam
            {
                ExamId = saved.ExamId,          // vigtigt: samme ID
                ExamName = "NewName",
                ClassId = saved.ClassId,        // behold valid FK
                ExamPatrol = true,
                IsReExam = true,
                IsFinalExam = true,
                NumOfStud = 99,
                Description = "New desc"
            };

            await _service.UpdateItemAsync(updated);

            // Assert
            var reloaded = await _service.GetItemByIdAsync(saved.ExamId);
            Assert.IsNotNull(reloaded);

            Assert.AreEqual("NewName", reloaded!.ExamName);
            Assert.AreEqual(99, reloaded.NumOfStud);
            Assert.AreEqual("New desc", reloaded.Description);
            Assert.IsTrue(reloaded.ExamPatrol);
            Assert.IsTrue(reloaded.IsReExam);
            Assert.IsTrue(reloaded.IsFinalExam);
        }

        [TestMethod]
        public async Task FilterByName_ReturnsCorrectExams()
        {
            // Arrange
            var cls = await CreateClassAsync();

            await _service.AddItemAsync(new Exam { ExamName = "PROGTEK1", ClassId = cls.ClassId });
            await _service.AddItemAsync(new Exam { ExamName = "SYSUDV", ClassId = cls.ClassId });
            await _service.AddItemAsync(new Exam { ExamName = "Database Design", ClassId = cls.ClassId });

            // Act (case-insensitive Contains)
            var filter = new GenericFilter { FilterByName = "data" };
            var result = (await _service.GetAllAsync(filter)).ToList();

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Database Design", result[0].ExamName);

            // Act: noget der matcher flere
            var filter2 = new GenericFilter { FilterByName = "pro" };
            var result2 = (await _service.GetAllAsync(filter2)).ToList();

            Assert.AreEqual(1, result2.Count);
            Assert.AreEqual("PROGTEK1", result2[0].ExamName);
        }

        [TestMethod]
        public async Task GetItemByIdAsync_ReturnsExam_WhenFound()
        {
            // Arrange
            var cls = await CreateClassAsync();

            await _service.AddItemAsync(new Exam { ExamName = "FindMe", ClassId = cls.ClassId });
            var saved = (await _service.GetAllAsync()).First();

            // Act
            var found = await _service.GetItemByIdAsync(saved.ExamId);

            // Assert
            Assert.IsNotNull(found);
            Assert.AreEqual(saved.ExamId, found!.ExamId);
            Assert.AreEqual("FindMe", found.ExamName);
        }

        [TestMethod]
        public async Task GetItemByIdAsync_ReturnsNull_WhenNotFound()
        {
            // Act
            var found = await _service.GetItemByIdAsync(999999);

            // Assert
            Assert.IsNull(found);
        }
    }
}
