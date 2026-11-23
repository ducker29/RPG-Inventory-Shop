using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RpgInventoryShop.Application;
using RpgInventoryShop.Domain;
using RpgInventoryShop.UnityAdapters;

namespace RpgInventoryShop.Adapters.Tests
{
    public sealed class JsonSaveStoreTests
    {
        private static readonly ItemDefinition Potion = new("potion", "Health Potion", 5, 3, 10);
        private static readonly ItemDefinition Sword = new("sword", "Sword", 15, 1);
        private const string PotionSlot = "{\"itemId\":\"potion\",\"quantity\":2}";
        private const string SwordSlot = "{\"itemId\":\"sword\",\"quantity\":1}";

        private string directory;
        private string file;
        private Inventory inventory;
        private GoldWallet wallet;
        private PlayerHealth health;
        private PlayerMana mana;
        private JsonSaveStore store;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "rpg-save-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            file = Path.Combine(directory, "save.json");

            inventory = new Inventory();
            inventory.TryAdd(Sword);
            wallet = new GoldWallet(10);
            health = new PlayerHealth(100, 35);
            mana = new PlayerMana(100, 48);
            store = new JsonSaveStore(file, new SaveState(inventory, wallet, health, mana, new TestCatalog()));
        }

        [TearDown]
        public void TearDown() => Directory.Delete(directory, recursive: true);

        [Test]
        public void RoundTripPreservesHolesStackPositionsGoldHealthAndMana()
        {
            for (int index = 0; index < 5; index++)
                inventory.TryAdd(Potion);
            inventory.TryRemoveAt(0);
            Assert.That(store.Save(), Is.EqualTo(SaveStatus.Success));

            string json = File.ReadAllText(file);
            Assert.That(json, Does.Contain("\"version\": 2").And.Contain("\"mp\": 48"));
            Assert.That(json, Does.Contain("\"itemId\": \"potion\""));
            Assert.That(json, Does.Not.Contain("Health Potion").And.Not.Contain("stackLimit"));

            inventory.TryRemoveAt(1);
            inventory.TryRemoveAt(2);
            inventory.TryAdd(Sword);
            wallet.RestoreBalance(99);
            health.RestoreHp(98);
            mana.RestoreMana(0);

            Assert.That(store.Load(), Is.EqualTo(SaveStatus.Success));
            Assert.That(inventory.GetSlot(0), Is.Null);
            Assert.That(inventory.GetSlot(1).Item, Is.SameAs(Potion));
            Assert.That(inventory.GetSlot(1).Quantity, Is.EqualTo(3));
            Assert.That(inventory.GetSlot(2).Quantity, Is.EqualTo(2));
            Assert.That(inventory.GetSlot(19), Is.Null);
            Assert.That(wallet.Balance, Is.EqualTo(10));
            Assert.That(health.CurrentHp, Is.EqualTo(35));
            Assert.That(mana.CurrentMana, Is.EqualTo(48));
        }

        [Test]
        public void LegacySaveWithoutManaRestoresFullManaAndCanBeSavedAsVersionTwo()
        {
            File.WriteAllText(file, Snapshot(PotionSlot, "null"));
            Assert.That(store.Load(), Is.EqualTo(SaveStatus.Success));
            Assert.That(inventory.GetSlot(0).Item, Is.SameAs(Potion));
            Assert.That(mana.CurrentMana, Is.EqualTo(mana.MaxMana));
            Assert.That(store.Save(), Is.EqualTo(SaveStatus.Success));
            Assert.That(File.ReadAllText(file), Does.Contain("\"version\": 2").And.Contain("\"mp\": 100"));
        }

        [TestCase("", SaveStatus.Corrupt)]
        [TestCase(",\"mp\":101", SaveStatus.InvalidState)]
        public void VersionTwoRequiresValidManaBeforeRestoringAnything(string mp, SaveStatus expected)
        {
            string legacy = Snapshot(PotionSlot, "null");
            File.WriteAllText(file, legacy.Replace("\"version\":1", "\"version\":2" + mp));
            Assert.That(store.Load(), Is.EqualTo(expected));
            AssertOriginalState();
            Assert.That(mana.CurrentMana, Is.EqualTo(48));
        }

        [Test]
        public void MissingSaveLeavesCurrentSessionIntact()
        {
            Assert.That(store.Load(), Is.EqualTo(SaveStatus.Missing));
            AssertOriginalState();
        }

        [TestCase("{", SaveStatus.IndeterminateVersion)]
        [TestCase("{\"version\":1,\"gold\":5,\"slots\":[]}", SaveStatus.Corrupt)]
        [TestCase("{\"version\":1,\"gold\":2147483648,\"hp\":5,\"slots\":[]}", SaveStatus.Corrupt)]
        [TestCase("{\"version\":1,\"gold\":5,\"hp\":5,\"slots\":[]}", SaveStatus.InvalidState)]
        [TestCase("{\"version\":0}", SaveStatus.UnsupportedVersion)]
        public void RejectsMalformedOrUnsupportedDocuments(string json, SaveStatus status)
        {
            File.WriteAllText(file, json);
            Assert.That(store.Load(), Is.EqualTo(status));
            AssertOriginalState();
        }

        [TestCase("{\"itemId\":\"missing\",\"quantity\":1}", SaveStatus.UnknownItem)]
        [TestCase("{\"itemId\":\"potion\",\"quantity\":0}", SaveStatus.InvalidState)]
        [TestCase("{\"itemId\":\"potion\",\"quantity\":-1}", SaveStatus.InvalidState)]
        [TestCase("{\"itemId\":\"potion\",\"quantity\":4}", SaveStatus.InvalidState)]
        [TestCase("{\"itemId\":\"sword\",\"quantity\":2}", SaveStatus.InvalidState)]
        [TestCase("{\"itemId\":\"\",\"quantity\":1}", SaveStatus.InvalidState)]
        [TestCase("{\"itemId\":\"\\uD800\",\"quantity\":1}", SaveStatus.Corrupt)]
        public void InvalidLastSlotDoesNotApplyAnyPartOfTheSnapshot(string lastSlot, SaveStatus status)
        {
            ItemStack original = inventory.GetSlot(0);
            File.WriteAllText(file, Snapshot(PotionSlot, lastSlot));

            Assert.That(store.Load(), Is.EqualTo(status));
            Assert.That(inventory.GetSlot(0), Is.SameAs(original));
            AssertOriginalState();
        }

        [TestCase(-1, 70)]
        [TestCase(40, -1)]
        [TestCase(40, 101)]
        public void InvalidPlayerValuesLeaveCurrentSessionIntact(int gold, int hp)
        {
            File.WriteAllText(file, Snapshot(PotionSlot, SwordSlot, gold, hp));
            Assert.That(store.Load(), Is.EqualTo(SaveStatus.InvalidState));
            AssertOriginalState();
        }

        [Test]
        public void ConflictingRegisteredDefinitionDoesNotApplyAnyPartOfTheSnapshot()
        {
            var conflicting = new ItemDefinition(Sword.Id, Sword.Name, 20, 1);
            var conflictingStore = new JsonSaveStore(
                file, new SaveState(inventory, wallet, health, mana, new TestCatalog(conflicting)));
            File.WriteAllText(file, Snapshot(PotionSlot, SwordSlot));

            Assert.That(conflictingStore.Load(), Is.EqualTo(SaveStatus.InvalidState));
            AssertOriginalState();
        }

        [TestCase("{\"version\":3,\"futureSchema\":true}")]
        [TestCase("{\"version\":2147483648}")]
        public void NewerPrimarySurvivesSaveEvenWithAValidBackup(string newer)
        {
            Assert.That(store.Save(), Is.EqualTo(SaveStatus.Success));
            File.Copy(file, file + ".bak");
            File.WriteAllText(file, newer);

            Assert.That(store.Load(), Is.EqualTo(SaveStatus.NewerVersion));
            Assert.That(store.Save(), Is.EqualTo(SaveStatus.NewerVersion));
            Assert.That(File.ReadAllText(file), Is.EqualTo(newer));
            AssertOriginalState();
        }

        [Test]
        public void NewerSaveBeyondDefaultParserDepthSurvivesSave()
        {
            const int depth = 80;
            string newer = "{\"version\":3,\"futureSchema\":" +
                new string('[', depth) + "0" + new string(']', depth) + "}";
            File.WriteAllText(file, newer);

            Assert.That(store.Load(), Is.EqualTo(SaveStatus.NewerVersion));
            Assert.That(store.Save(), Is.EqualTo(SaveStatus.NewerVersion));
            Assert.That(File.ReadAllText(file), Is.EqualTo(newer));
            AssertOriginalState();
        }

        [TestCase("{")]
        [TestCase("{\"gold\":10}")]
        [TestCase("{\"version\":\"2\"}")]
        [TestCase("{\"version\":2,\"version\":1}")]
        public void CannotOverwriteFileWithIndeterminateVersion(string content)
        {
            File.WriteAllText(file, content);

            Assert.That(store.Load(), Is.EqualTo(SaveStatus.IndeterminateVersion));
            Assert.That(store.Save(), Is.EqualTo(SaveStatus.IndeterminateVersion));
            Assert.That(File.ReadAllText(file), Is.EqualTo(content));
            Assert.That(File.Exists(file + ".bak"), Is.False);
            Assert.That(Directory.GetFiles(directory, "*.tmp"), Is.Empty);
            AssertOriginalState();
        }

        [Test]
        public void CannotRotateBackupWithIndeterminateVersion()
        {
            Assert.That(store.Save(), Is.EqualTo(SaveStatus.Success));
            string original = File.ReadAllText(file);
            File.WriteAllText(file + ".bak", "{");

            Assert.That(store.Save(), Is.EqualTo(SaveStatus.IndeterminateVersion));
            Assert.That(File.ReadAllText(file), Is.EqualTo(original));
            Assert.That(File.ReadAllText(file + ".bak"), Is.EqualTo("{"));
        }

        [Test]
        public void NewerBackupIsNeverOverwritten()
        {
            Assert.That(store.Save(), Is.EqualTo(SaveStatus.Success));
            string original = File.ReadAllText(file);
            const string newer = "{\"version\":3}";
            File.WriteAllText(file + ".bak", newer);

            Assert.That(store.Save(), Is.EqualTo(SaveStatus.NewerVersion));
            Assert.That(File.ReadAllText(file), Is.EqualTo(original));
            Assert.That(File.ReadAllText(file + ".bak"), Is.EqualTo(newer));
        }

        [Test]
        public void RecoversPreviousSaveWithoutOverwritingUnidentifiablePrimary()
        {
            Assert.That(store.Save(), Is.EqualTo(SaveStatus.Success));
            string original = File.ReadAllText(file);
            wallet.RestoreBalance(30);
            Assert.That(store.Save(), Is.EqualTo(SaveStatus.Success));
            Assert.That(File.ReadAllText(file + ".bak"), Is.EqualTo(original));
            File.WriteAllText(file, "{");

            Assert.That(store.Load(), Is.EqualTo(SaveStatus.RecoveredBackup));
            AssertOriginalState();
            Assert.That(store.Save(), Is.EqualTo(SaveStatus.IndeterminateVersion));
            Assert.That(File.ReadAllText(file), Is.EqualTo("{"));
            Assert.That(File.ReadAllText(file + ".bak"), Is.EqualTo(original));
        }

        [Test]
        public void PreservesRecoverableBackupAcrossSaveAfterConflictingPrimary()
        {
            var conflicting = new ItemDefinition(Sword.Id, Sword.Name, 20, 1);
            var conflictingStore = new JsonSaveStore(
                file, new SaveState(inventory, wallet, health, mana, new TestCatalog(conflicting)));
            File.WriteAllText(file, Snapshot(PotionSlot, SwordSlot));
            string originalBackup = Snapshot(PotionSlot, "null");
            File.WriteAllText(file + ".bak", originalBackup);

            Assert.That(conflictingStore.Load(), Is.EqualTo(SaveStatus.RecoveredBackup));
            Assert.That(inventory.GetSlot(0).Quantity, Is.EqualTo(2));
            Assert.That(wallet.Balance, Is.EqualTo(40));
            Assert.That(health.CurrentHp, Is.EqualTo(70));
            wallet.RestoreBalance(55);
            Assert.That(conflictingStore.Save(), Is.EqualTo(SaveStatus.Success));
            Assert.That(File.ReadAllText(file + ".bak"), Is.EqualTo(originalBackup));
            Assert.That(conflictingStore.Load(), Is.EqualTo(SaveStatus.Success));
            Assert.That(wallet.Balance, Is.EqualTo(55));

            File.WriteAllText(file, "{");
            inventory.TryRemoveAt(0);
            health.RestoreHp(10);

            Assert.That(conflictingStore.Load(), Is.EqualTo(SaveStatus.RecoveredBackup));
            Assert.That(inventory.GetSlot(0).Quantity, Is.EqualTo(2));
            Assert.That(wallet.Balance, Is.EqualTo(40));
            Assert.That(health.CurrentHp, Is.EqualTo(70));
        }

        private void AssertOriginalState()
        {
            Assert.That(inventory.GetSlot(0).Item, Is.SameAs(Sword));
            Assert.That(inventory.GetSlot(0).Quantity, Is.EqualTo(1));
            for (int index = 1; index < Inventory.SlotCount; index++)
                Assert.That(inventory.GetSlot(index), Is.Null, $"Slot {index}");
            Assert.That(wallet.Balance, Is.EqualTo(10));
            Assert.That(health.CurrentHp, Is.EqualTo(35));
        }

        private static string Snapshot(string first, string last, int gold = 40, int hp = 70)
        {
            string empty = string.Join(",", Enumerable.Repeat("null", Inventory.SlotCount - 2));
            return FormattableString.Invariant(
                $"{{\"version\":1,\"gold\":{gold},\"hp\":{hp},\"slots\":[{first},{empty},{last}]}}");
        }

        private sealed class TestCatalog : IItemCatalog
        {
            private readonly ItemDefinition sword;

            public TestCatalog(ItemDefinition sword = null) => this.sword = sword ?? Sword;

            public bool TryGet(string itemId, out ItemDefinition item)
            {
                item = itemId switch
                {
                    "potion" => Potion,
                    "sword" => sword,
                    _ => null
                };
                return item != null;
            }
        }
    }
}
