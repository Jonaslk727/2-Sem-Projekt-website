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
    public class EFRoomServiceTests
    {
        private EksamensDBContext _context = null!;
        private EFRoomService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<EksamensDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // isoleret DB pr test
                .Options;

            _context = new EksamensDBContext(options);
            _service = new EFRoomService(_context);
        }

        [TestMethod]
        public async Task Room_CanBeSaved_AndDeleted()
        {
            // Arrange
            var room = new Room { Name = "A101", Capacity = 30 };

            // Act - Save
            await _service.AddItemAsync(room);

            // Assert - saved
            var allAfterAdd = await _service.GetAllAsync();
            Assert.AreEqual(1, allAfterAdd.Count());

            var saved = allAfterAdd.First();
            Assert.AreEqual("A101", saved.Name);
            Assert.AreEqual(30, saved.Capacity);

            // Act - Delete
            await _service.DeleteItemAsync(saved.RoomId);

            // Assert - deleted
            var allAfterDelete = await _service.GetAllAsync();
            Assert.AreEqual(0, allAfterDelete.Count());
        }

        [TestMethod]
        public async Task Room_CanBeUpdated_NameAndCapacity()
        {
            // Arrange
            var room = new Room { Name = "OldName", Capacity = 10 };
            await _service.AddItemAsync(room);

            var saved = (await _service.GetAllAsync()).First();

            // Act
            var updated = new Room
            {
                RoomId = saved.RoomId,
                Name = "NewName",
                Capacity = 99
            };

            await _service.UpdateItemAsync(updated);

            // Assert
            var reloaded = await _service.GetItemByIdAsync(saved.RoomId);
            Assert.IsNotNull(reloaded);
            Assert.AreEqual("NewName", reloaded!.Name);
            Assert.AreEqual(99, reloaded.Capacity);
        }

        [TestMethod]
        public async Task FilterByName_ReturnsCorrectRooms()
        {
            // Arrange
            await _service.AddItemAsync(new Room { Name = "Alpha", Capacity = 10 });
            await _service.AddItemAsync(new Room { Name = "Beta", Capacity = 20 });
            await _service.AddItemAsync(new Room { Name = "A101", Capacity = 30 });

            // Act
            var filter = new GenericFilter { FilterByName = "a" }; // skal matche Alpha + A101 + Beta? (Beta indeholder også 'a')
            var result = (await _service.GetAllAsync(filter)).ToList();

            // Assert
            // VIGTIGT: jeres filter er Contains() på navn (case-insensitive) :contentReference[oaicite:3]{index=3}
            // "a" findes i "Alpha", "A101" OG "Beta" -> derfor 3
            Assert.AreEqual(3, result.Count);

            Assert.IsTrue(result.Any(r => r.Name == "Alpha"));
            Assert.IsTrue(result.Any(r => r.Name == "A101"));
            Assert.IsTrue(result.Any(r => r.Name == "Beta"));
        }

        [TestMethod]
        public async Task GetItemByIdAsync_ReturnsRoom_WhenIdExists()
        {
            // Arrange
            await _service.AddItemAsync(new Room { Name = "RoomX", Capacity = 5 });
            var saved = (await _service.GetAllAsync()).First();

            // Act
            var found = await _service.GetItemByIdAsync(saved.RoomId);

            // Assert
            Assert.IsNotNull(found);
            Assert.AreEqual(saved.RoomId, found!.RoomId);
            Assert.AreEqual("RoomX", found.Name);
        }

        [TestMethod]
        public async Task GetItemByIdAsync_ReturnsNull_WhenIdDoesNotExist()
        {
            // Act
            var found = await _service.GetItemByIdAsync(999999);

            // Assert
            Assert.IsNull(found);
        }
    }
}
