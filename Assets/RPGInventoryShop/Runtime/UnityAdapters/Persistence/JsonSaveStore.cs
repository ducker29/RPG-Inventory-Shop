using System;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text.Json;
using RpgInventoryShop.Application;
using RpgInventoryShop.Domain;

namespace RpgInventoryShop.UnityAdapters
{
    // Use one store instance per save path in the owning process.
    public sealed class JsonSaveStore
    {
        private const int MaxReadDepth = 256;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        private readonly string path;
        private readonly string backupPath;
        private readonly SaveState state;
        private readonly object operationLock = new();

        public JsonSaveStore(SaveState state)
            : this(Path.Combine(UnityEngine.Application.persistentDataPath, "inventory-save.json"), state)
        {
        }

        public JsonSaveStore(string path, SaveState state)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("A save file path is required.", nameof(path));

            this.path = Path.GetFullPath(path);
            backupPath = this.path + ".bak";
            this.state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public SaveStatus Save()
        {
            lock (operationLock)
                return SaveCore();
        }

        private SaveStatus SaveCore()
        {
            SaveStatus primary = Read(path, out SaveData previous);
            SaveStatus backup = Read(backupPath, out _);
            if (primary == SaveStatus.NewerVersion || backup == SaveStatus.NewerVersion)
                return SaveStatus.NewerVersion;
            if (primary == SaveStatus.IndeterminateVersion || backup == SaveStatus.IndeterminateVersion)
                return SaveStatus.IndeterminateVersion;
            if (primary == SaveStatus.IoError || backup == SaveStatus.IoError)
                return SaveStatus.IoError;

            string temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write))
                {
                    JsonSerializer.Serialize(stream, state.Capture(), JsonOptions);
                    stream.Flush(flushToDisk: true);
                }

                if (primary == SaveStatus.Missing)
                    File.Move(temporaryPath, path);
                else
                    File.Replace(temporaryPath, path,
                        primary == SaveStatus.Success && state.Validate(previous) == SaveStatus.Success
                            ? backupPath
                            : null);

                return SaveStatus.Success;
            }
            catch (IOException)
            {
                return SaveStatus.IoError;
            }
            catch (UnauthorizedAccessException)
            {
                return SaveStatus.IoError;
            }
            finally
            {
                try { File.Delete(temporaryPath); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        public SaveStatus Load()
        {
            lock (operationLock)
                return LoadCore();
        }

        private SaveStatus LoadCore()
        {
            SaveStatus primary = Restore(path);
            if (primary == SaveStatus.Success || primary == SaveStatus.NewerVersion ||
                primary == SaveStatus.UnsupportedVersion || primary == SaveStatus.IoError)
                return primary;

            SaveStatus backup = Restore(backupPath);
            if (backup == SaveStatus.Success)
                return SaveStatus.RecoveredBackup;
            return primary == SaveStatus.Missing || backup == SaveStatus.NewerVersion ? backup : primary;
        }

        private SaveStatus Restore(string file)
        {
            SaveStatus status = Read(file, out SaveData data);
            return status == SaveStatus.Success ? state.Restore(data) : status;
        }

        private static SaveStatus Read(string file, out SaveData data)
        {
            data = null;
            try
            {
                using var stream = File.OpenRead(file);
                using JsonDocument document = JsonDocument.Parse(
                    stream, new JsonDocumentOptions { MaxDepth = MaxReadDepth });
                JsonElement root = document.RootElement;
                if (!TryReadVersion(root, out BigInteger versionNumber))
                    return SaveStatus.IndeterminateVersion;

                // Check the version before interpreting any fields from the current schema.
                if (versionNumber > SaveData.CurrentVersion)
                    return SaveStatus.NewerVersion;
                if (versionNumber != SaveData.CurrentVersion)
                    return SaveStatus.UnsupportedVersion;

                if (!TryReadInt(root, "gold", out int gold) || !TryReadInt(root, "hp", out int hp) ||
                    !root.TryGetProperty("slots", out JsonElement slots) || slots.ValueKind != JsonValueKind.Array)
                    return SaveStatus.Corrupt;
                if (slots.GetArrayLength() != Inventory.SlotCount)
                    return SaveStatus.InvalidState;

                var savedSlots = new SavedSlot[Inventory.SlotCount];
                int index = 0;
                foreach (JsonElement slot in slots.EnumerateArray())
                {
                    if (slot.ValueKind != JsonValueKind.Null)
                    {
                        if (slot.ValueKind != JsonValueKind.Object ||
                            !slot.TryGetProperty("itemId", out JsonElement id) || id.ValueKind != JsonValueKind.String ||
                            !TryReadInt(slot, "quantity", out int quantity))
                            return SaveStatus.Corrupt;

                        string itemId;
                        try
                        {
                            itemId = id.GetString();
                        }
                        catch (InvalidOperationException)
                        {
                            // A parsed string can still contain invalid Unicode escapes.
                            return SaveStatus.Corrupt;
                        }

                        savedSlots[index] = new SavedSlot { ItemId = itemId, Quantity = quantity };
                    }

                    index++;
                }

                data = new SaveData { Version = SaveData.CurrentVersion, Gold = gold, Hp = hp, Slots = savedSlots };
                return SaveStatus.Success;
            }
            catch (FileNotFoundException) { return SaveStatus.Missing; }
            catch (DirectoryNotFoundException) { return SaveStatus.Missing; }
            catch (JsonException) { return SaveStatus.IndeterminateVersion; }
            catch (IOException) { return SaveStatus.IoError; }
            catch (UnauthorizedAccessException) { return SaveStatus.IoError; }
        }

        private static bool TryReadVersion(JsonElement root, out BigInteger value)
        {
            value = default;
            if (root.ValueKind != JsonValueKind.Object)
                return false;

            bool found = false;
            try
            {
                foreach (JsonProperty property in root.EnumerateObject())
                {
                    if (!property.NameEquals("version"))
                        continue;
                    if (found || property.Value.ValueKind != JsonValueKind.Number ||
                        !BigInteger.TryParse(property.Value.GetRawText(), NumberStyles.AllowLeadingSign,
                            CultureInfo.InvariantCulture, out value))
                        return false;

                    found = true;
                }
            }
            catch (InvalidOperationException)
            {
                // Invalid Unicode in a property name can prevent reliable version detection.
                return false;
            }

            return found;
        }

        private static bool TryReadInt(JsonElement parent, string property, out int value)
        {
            value = default;
            return parent.TryGetProperty(property, out JsonElement element) &&
                element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out value);
        }
    }
}
