using GhostBodyObject.Common.Utilities;
using GhostBodyObject.HandWritten.Blogger;
using GhostBodyObject.HandWritten.Blogger.Repository;
using GhostBodyObject.HandWritten.BloggerApp.Entities.User;
using GhostBodyObject.Repository.Ghost.Constants;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository.Transaction.Index;
using System;
using System.Collections.Generic;

namespace GhostBodyObject.Repository.Tests.Repository.Transaction
{
    public unsafe class TransactionBodyMapShould
    {
        private BloggerRepository CreateRepository() => new BloggerRepository();

        private BloggerUser CreateUser(BloggerRepository repository)
        {
            var user = new BloggerUser();
            user.Active = true;
            return user;
        }

        [Fact]
        public void InsertBody()
        {
            var index = new TransactionBodyMap<BloggerUser>();
            var repository = CreateRepository();
            using (BloggerContext.NewWriteContext(repository))
            {
                var user = CreateUser(repository);

                index.Set(user);

                Assert.Equal(1, index.Count);
            }
        }

        [Fact]
        public void InsertAndRetrieveBody()
        {
            var index = new TransactionBodyMap<BloggerUser>();
            var repository = CreateRepository();
            using (BloggerContext.NewWriteContext(repository))
            {
                var user = CreateUser(repository);
                var id = user.Header->Id;

                index.Set(user);
                var retrieved = index.Get(id, out bool exists);

                Assert.True(exists);
                Assert.Same(user, retrieved);
            }
        }

        [Fact]
        public void ReturnNotExistsForMissingId()
        {
            var index = new TransactionBodyMap<BloggerUser>();
            var repository = CreateRepository();
            using (BloggerContext.NewWriteContext(repository))
            {
                var user = CreateUser(repository);
                index.Set(user);

                // Create a different ID that doesn't exist
                var otherId = GhostId.NewId(GhostIdKind.Entity, 1);
                var retrieved = index.Get(otherId, out bool exists);

                Assert.False(exists);
                Assert.Null(retrieved);
            }
        }

        [Fact]
        public void UpdateExistingEntry()
        {
            var index = new TransactionBodyMap<BloggerUser>();
            var repository = CreateRepository();
            using (BloggerContext.NewWriteContext(repository))
            {
                var user1 = CreateUser(repository);
                var id = user1.Header->Id;

                index.Set(user1);
                Assert.Equal(1, index.Count);

                // Create another user with the same ID (simulate update)
                var user2 = CreateUser(repository);
                // Copy the ID to simulate an update
                *(user2.Header) = *(user1.Header);

                index.Set(user2);

                // Count should remain 1
                Assert.Equal(1, index.Count);
                var retrieved = index.Get(id, out bool exists);
                Assert.True(exists);
                Assert.Same(user2, retrieved);
            }
        }

        [Fact]
        public void InsertMultipleBodies()
        {
            var index = new TransactionBodyMap<BloggerUser>();
            var repository = CreateRepository();
            using (BloggerContext.NewWriteContext(repository))
            {
                const int count = 100;
                var users = new BloggerUser[count];
                var ids = new GhostId[count];

                for (int i = 0; i < count; i++)
                {
                    users[i] = CreateUser(repository);
                    ids[i] = users[i].Header->Id;
                    index.Set(users[i]);
                }

                Assert.Equal(count, index.Count);

                // Verify all can be retrieved
                for (int i = 0; i < count; i++)
                {
                    var retrieved = index.Get(ids[i], out bool exists);
                    Assert.True(exists);
                    Assert.Same(users[i], retrieved);
                }
            }
        }

        [Fact]
        public void RemoveExistingEntry()
        {
            var index = new TransactionBodyMap<BloggerUser>();
            var repository = CreateRepository();
            using (BloggerContext.NewWriteContext(repository))
            {
                var user = CreateUser(repository);
                var id = user.Header->Id;

                index.Set(user);
                Assert.Equal(1, index.Count);

                bool removed = index.Remove(id);

                Assert.True(removed);
                Assert.Equal(0, index.Count);
                var retrieved = index.Get(id, out bool exists);
                Assert.False(exists);
            }
        }

        [Fact]
        public void RemoveNonExistingEntryReturnsFalse()
        {
            var index = new TransactionBodyMap<BloggerUser>();
            var repository = CreateRepository();
            using (BloggerContext.NewWriteContext(repository))
            {
                var user = CreateUser(repository);
                index.Set(user);

                var otherId = GhostId.NewId(GhostIdKind.Entity, 1);
                bool removed = index.Remove(otherId);

                Assert.False(removed);
                Assert.Equal(1, index.Count);
            }
        }

        [Fact]
        public void RemoveMultipleEntries()
        {
            var index = new TransactionBodyMap<BloggerUser>();
            var repository = CreateRepository();
            using (BloggerContext.NewWriteContext(repository))
            {
                const int count = 50;
                var users = new BloggerUser[count];
                var ids = new GhostId[count];

                for (int i = 0; i < count; i++)
                {
                    users[i] = CreateUser(repository);
                    ids[i] = users[i].Header->Id;
                    index.Set(users[i]);
                }

                Assert.Equal(count, index.Count);

                // Remove every other entry
                for (int i = 0; i < count; i += 2)
                {
                    bool removed = index.Remove(ids[i]);
                    Assert.True(removed);
                }

                Assert.Equal(count / 2, index.Count);

                // Verify removed entries are gone and others remain
                for (int i = 0; i < count; i++)
                {
                    var retrieved = index.Get(ids[i], out bool exists);
                    if (i % 2 == 0)
                    {
                        Assert.False(exists);
                    }
                    else
                    {
                        Assert.True(exists);
                        Assert.Same(users[i], retrieved);
                    }
                }
            }
        }

        [Fact]
        public void ResizeOnHighLoad()
        {
            // Start with small capacity to trigger resize
            var index = new TransactionBodyMap<BloggerUser>(16);
            var repository = CreateRepository();
            using (BloggerContext.NewWriteContext(repository))
            {
                int initialCapacity = index.Capacity;
                const int count = 100; // More than 75% of 16

                var users = new BloggerUser[count];
                var ids = new GhostId[count];

                for (int i = 0; i < count; i++)
                {
                    users[i] = CreateUser(repository);
                    ids[i] = users[i].Header->Id;
                    index.Set(users[i]);
                }

                // Capacity should have grown
                Assert.True(index.Capacity > initialCapacity);
                Assert.Equal(count, index.Count);

                // All entries should still be retrievable
                for (int i = 0; i < count; i++)
                {
                    var retrieved = index.Get(ids[i], out bool exists);
                    Assert.True(exists);
                    Assert.Same(users[i], retrieved);
                }
            }
        }

        [Fact]
        public void ShrinkOnLowLoad()
        {
            var index = new TransactionBodyMap<BloggerUser>(16);
            var repository = CreateRepository();
            using (BloggerContext.NewWriteContext(repository))
            {
                const int count = 100;
                var users = new BloggerUser[count];
                var ids = new GhostId[count];

                // Fill the index to trigger growth
                for (int i = 0; i < count; i++)
                {
                    users[i] = CreateUser(repository);
                    ids[i] = users[i].Header->Id;
                    index.Set(users[i]);
                }

                int capacityAfterGrowth = index.Capacity;

                // Remove most entries to trigger shrink
                for (int i = 0; i < count - 5; i++)
                {
                    index.Remove(ids[i]);
                }

                // Capacity should have shrunk (but not below initial capacity)
                Assert.True(index.Capacity < capacityAfterGrowth || index.Capacity == 16);
                Assert.Equal(5, index.Count);

                // Remaining entries should still be retrievable
                for (int i = count - 5; i < count; i++)
                {
                    var retrieved = index.Get(ids[i], out bool exists);
                    Assert.True(exists);
                    Assert.Same(users[i], retrieved);
                }
            }
        }

        [Fact]
        public void EnumerateAllEntries()
        {
            var index = new TransactionBodyMap<BloggerUser>();
            var repository = CreateRepository();
            using (BloggerContext.NewWriteContext(repository))
            {
                const int count = 50;
                var users = new HashSet<BloggerUser>();

                for (int i = 0; i < count; i++)
                {
                    var user = CreateUser(repository);
                    users.Add(user);
                    index.Set(user);
                }

                var enumeratedUsers = new HashSet<BloggerUser>();
                var enumerator = index.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    enumeratedUsers.Add(enumerator.Current);
                }

                Assert.Equal(count, enumeratedUsers.Count);
                Assert.True(users.SetEquals(enumeratedUsers));
            }
        }

        [Fact]
        public void EnumerateEmptyIndex()
        {
            var index = new TransactionBodyMap<BloggerUser>();

            int count = 0;
            var enumerator = index.GetEnumerator();
            while (enumerator.MoveNext())
            {
                count++;
            }

            Assert.Equal(0, count);
        }

        [Fact]
        public void GetEntriesArrayReturnsInternalArray()
        {
            var index = new TransactionBodyMap<BloggerUser>();
            var repository = CreateRepository();
            using (BloggerContext.NewWriteContext(repository))
            {
                var user = CreateUser(repository);
                index.Set(user);

                var entries = index.GetEntriesArray();

                Assert.NotNull(entries);
                Assert.Equal(index.Capacity, entries.Length);

                // Count non-null entries
                int nonNullCount = 0;
                foreach (var entry in entries)
                {
                    if (entry != null) nonNullCount++;
                }
                Assert.Equal(1, nonNullCount);
            }
        }

        [Fact]
        public void ClearRemovesAllEntries()
        {
            var index = new TransactionBodyMap<BloggerUser>();
            var repository = CreateRepository();
            using (BloggerContext.NewWriteContext(repository))
            {
                const int count = 20;
                var ids = new GhostId[count];

                for (int i = 0; i < count; i++)
                {
                    var user = CreateUser(repository);
                    ids[i] = user.Header->Id;
                    index.Set(user);
                }

                Assert.Equal(count, index.Count);

                index.Clear();

                Assert.Equal(0, index.Count);

                // Verify all entries are gone
                for (int i = 0; i < count; i++)
                {
                    var retrieved = index.Get(ids[i], out bool exists);
                    Assert.False(exists);
                }
            }
        }

        [Fact]
        public void HandleCollisions()
        {
            // Insert many entries to ensure collisions occur
            var index = new TransactionBodyMap<BloggerUser>(16);
            var repository = CreateRepository();
            using (BloggerContext.NewWriteContext(repository))
            {
                const int count = 200;
                var users = new BloggerUser[count];
                var ids = new GhostId[count];

                for (int i = 0; i < count; i++)
                {
                    users[i] = CreateUser(repository);
                    ids[i] = users[i].Header->Id;
                    index.Set(users[i]);
                }

                Assert.Equal(count, index.Count);

                // All entries should be retrievable despite collisions
                for (int i = 0; i < count; i++)
                {
                    var retrieved = index.Get(ids[i], out bool exists);
                    Assert.True(exists, $"Entry at index {i} should exist");
                    Assert.Same(users[i], retrieved);
                }
            }
        }

        [Fact]
        public void RemoveWithCollisions()
        {
            // Insert entries and remove some to test ShiftBack with collisions
            var index = new TransactionBodyMap<BloggerUser>(16);
            var repository = CreateRepository();
            using (BloggerContext.NewWriteContext(repository))
            {
                const int count = 100;
                var users = new BloggerUser[count];
                var ids = new GhostId[count];

                for (int i = 0; i < count; i++)
                {
                    users[i] = CreateUser(repository);
                    ids[i] = users[i].Header->Id;
                    index.Set(users[i]);
                }

                // Remove entries in various positions
                var removeIndices = new[] { 0, 25, 50, 75, 99 };
                foreach (var idx in removeIndices)
                {
                    bool removed = index.Remove(ids[idx]);
                    Assert.True(removed);
                }

                Assert.Equal(count - removeIndices.Length, index.Count);

                // Remaining entries should still be retrievable
                for (int i = 0; i < count; i++)
                {
                    var retrieved = index.Get(ids[i], out bool exists);
                    if (Array.IndexOf(removeIndices, i) >= 0)
                    {
                        Assert.False(exists);
                    }
                    else
                    {
                        Assert.True(exists, $"Entry at index {i} should exist");
                        Assert.Same(users[i], retrieved);
                    }
                }
            }
        }

        [Fact]
        public void InitialCapacityIsRespected()
        {
            var index16 = new TransactionBodyMap<BloggerUser>(16);
            var index32 = new TransactionBodyMap<BloggerUser>(32);
            var index100 = new TransactionBodyMap<BloggerUser>(100);

            Assert.Equal(16, index16.Capacity);
            Assert.Equal(32, index32.Capacity);
            Assert.Equal(128, index100.Capacity); // Rounded up to power of 2
        }

        [Fact]
        public void MinimumCapacityIsEnforced()
        {
            var index = new TransactionBodyMap<BloggerUser>(1);
            Assert.Equal(16, index.Capacity); // Minimum is 16
        }

        [Fact]
        public void EnumeratorForeachPattern()
        {
            var index = new TransactionBodyMap<BloggerUser>();
            var repository = CreateRepository();
            using (BloggerContext.NewWriteContext(repository))
            {
                const int count = 10;
                var userSet = new HashSet<BloggerUser>();

                for (int i = 0; i < count; i++)
                {
                    var user = CreateUser(repository);
                    userSet.Add(user);
                    index.Set(user);
                }

                // Test using the struct enumerator directly
                var foundUsers = new List<BloggerUser>();
                foreach (var user in index)
                {
                    foundUsers.Add(user);
                }

                Assert.Equal(count, foundUsers.Count);
            }
        }
    }
}
