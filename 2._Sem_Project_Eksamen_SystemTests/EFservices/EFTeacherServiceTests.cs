using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

using _2._Sem_Project_Eksamen_System.EFservices;
using _2._Sem_Project_Eksamen_System.Models1;
using _2._Sem_Project_Eksamen_System.Utils;

namespace YourTestProjectName
{
    [TestClass]
    public class EFTeacherServiceTests
    {
        private EksamensDBContext _context = null!;
        private EFTeacherService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<EksamensDBContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // isoleret DB pr test
                .Options;

            _context = new EksamensDBContext(options);
            _service = new EFTeacherService(_context);
        }

        [TestMethod]
        public async Task Teacher_CanBeSaved_AndDeleted()
        {
            // Arrange
            var teacher = new Teacher
            {
                TeacherName = "Anders Hansen",
                Email = "anders@zealand.dk"
            };

            // Act - Save
            await _service.AddItemAsync(teacher);

            // Assert - saved
            var allAfterAdd = (await _service.GetAllAsync()).ToList();
            Assert.AreEqual(1, allAfterAdd.Count);

            var saved = allAfterAdd.First();
            Assert.AreEqual("Anders Hansen", saved.TeacherName);
            Assert.AreEqual("anders@zealand.dk", saved.Email);

            // Act - Delete
            await _service.DeleteItemAsync(saved.TeacherId);

            // Assert - deleted
            var allAfterDelete = (await _service.GetAllAsync()).ToList();
            Assert.AreEqual(0, allAfterDelete.Count);
        }

        [TestMethod]
        public async Task Teacher_CanBeUpdated_WhenExists()
        {
            // Arrange
            await _service.AddItemAsync(new Teacher { TeacherName = "Old Name", Email = "old@zealand.dk" });
            var saved = (await _service.GetAllAsync()).First();

            // Act
            var updated = new Teacher
            {
                TeacherId = saved.TeacherId,
                TeacherName = "New Name",
                Email = "new@zealand.dk"
            };

            await _service.UpdateItemAsync(updated);

            // Assert
            var reloaded = await _service.GetItemByIdAsync(saved.TeacherId);
            Assert.IsNotNull(reloaded);
            Assert.AreEqual("New Name", reloaded!.TeacherName);
            Assert.AreEqual("new@zealand.dk", reloaded.Email);
        }

        [TestMethod]
        public async Task FilterByNameOrEmail_ReturnsCorrectResults()
        {
            // Arrange
            await _service.AddItemAsync(new Teacher { TeacherName = "Alice A", Email = "alice@zealand.dk" });
            await _service.AddItemAsync(new Teacher { TeacherName = "Bob B", Email = "bob@school.dk" });
            await _service.AddItemAsync(new Teacher { TeacherName = "Charlie C", Email = null });

            // Act 1: filter på navn
            var filterName = new GenericFilter { FilterByName = "bob" };
            var byName = (await _service.GetAllAsync(filterName)).ToList();

            // Assert 1
            Assert.AreEqual(1, byName.Count);
            Assert.AreEqual("Bob B", byName[0].TeacherName);

            // Act 2: filter på email
            var filterEmail = new GenericFilter { FilterByName = "zealand.dk" };
            var byEmail = (await _service.GetAllAsync(filterEmail)).ToList();

            // Assert 2
            Assert.AreEqual(1, byEmail.Count);
            Assert.AreEqual("Alice A", byEmail[0].TeacherName);

            // Act 3: term der ikke findes
            var filterNone = new GenericFilter { FilterByName = "does-not-exist" };
            var none = (await _service.GetAllAsync(filterNone)).ToList();

            // Assert 3
            Assert.AreEqual(0, none.Count);
        }

        [TestMethod]
        public async Task GetItemByIdAsync_ReturnsTeacher_WhenFound()
        {
            // Arrange
            await _service.AddItemAsync(new Teacher { TeacherName = "Find Me", Email = "find@x.dk" });
            var saved = (await _service.GetAllAsync()).First();

            // Act
            var found = await _service.GetItemByIdAsync(saved.TeacherId);

            // Assert
            Assert.IsNotNull(found);
            Assert.AreEqual(saved.TeacherId, found!.TeacherId);
            Assert.AreEqual("Find Me", found.TeacherName);
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
