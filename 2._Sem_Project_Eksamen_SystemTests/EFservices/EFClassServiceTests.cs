using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

using _2._Sem_Project_Eksamen_System.EFservices;
using _2._Sem_Project_Eksamen_System.Models1;

namespace YourTestProjectName
{
    [TestClass]
    public class EFClassServiceTests
    {
        private EksamensDBContext _context = null!;
        private EFClassService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<EksamensDBContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new EksamensDBContext(options);
            _service = new EFClassService(_context);
        }

        [TestMethod]
        public async Task Class_CanBeAdded_AndDeleted()
        {
            // Arrange
            // Skal matche jeres regex/normalizer i ClassName :contentReference[oaicite:1]{index=1}
            var newClass = new Class
            {
                ClassName = "Data-RO-F-V25B-2sem"
            };

            // Act - Add
            await _service.AddItemAsync(newClass);

            // Assert - Added
            var allAfterAdd = (await _service.GetAllAsync()).ToList();
            Assert.AreEqual(1, allAfterAdd.Count);

            var saved = allAfterAdd.First();
            Assert.AreEqual("Data-RO-F-V25B-2sem", saved.ClassName);

            // Act - Delete
            await _service.DeleteItemAsync(saved.ClassId);

            // Assert - Deleted
            var allAfterDelete = (await _service.GetAllAsync()).ToList();
            Assert.AreEqual(0, allAfterDelete.Count);
        }

        [TestMethod]
        public async Task Delete_DoesNothing_WhenClassNotFound()
        {
            // Act + Assert (skal ikke kaste exception)
            await _service.DeleteItemAsync(999999);

            var all = (await _service.GetAllAsync()).ToList();
            Assert.AreEqual(0, all.Count);
        }
    }
}
