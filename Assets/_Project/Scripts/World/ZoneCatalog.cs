using System;
using System.Collections.Generic;
using UnityEngine;

namespace EscapeUNIFRANZ.World
{
    [Serializable]
    public sealed class ZoneDefinition
    {
        [SerializeField] private string zoneId;
        [SerializeField] private string scenePath;

        public ZoneDefinition(string zoneId, string scenePath)
        {
            this.zoneId = zoneId;
            this.scenePath = scenePath;
        }

        public string ZoneId => zoneId;
        public string ScenePath => scenePath;
    }

    [CreateAssetMenu(menuName = "Escape UNIFRANZ/Zone Catalog", fileName = "ZoneCatalog")]
    public sealed class ZoneCatalog : ScriptableObject
    {
        [SerializeField] private List<ZoneDefinition> zones = new List<ZoneDefinition>();

        public IReadOnlyList<ZoneDefinition> Zones => zones;

        public bool TryResolveScene(string zoneId, out string scenePath, out string error)
        {
            return TryResolveScene(zones, zoneId, out scenePath, out error);
        }

        public static bool TryResolveScene(
            IReadOnlyList<ZoneDefinition> definitions,
            string zoneId,
            out string scenePath,
            out string error)
        {
            if (!TryCreateLookup(definitions, out Dictionary<string, string> lookup, out error))
            {
                scenePath = null;
                return false;
            }

            if (!lookup.TryGetValue(zoneId ?? string.Empty, out scenePath))
            {
                error = $"ZoneId '{zoneId}' does not exist in the catalog.";
                return false;
            }

            error = null;
            return true;
        }

        public static bool TryCreateLookup(
            IReadOnlyList<ZoneDefinition> definitions,
            out Dictionary<string, string> lookup,
            out string error)
        {
            lookup = new Dictionary<string, string>(StringComparer.Ordinal);

            if (definitions == null)
            {
                error = "Zone definitions are null.";
                return false;
            }

            for (int index = 0; index < definitions.Count; index++)
            {
                ZoneDefinition definition = definitions[index];
                if (definition == null || string.IsNullOrWhiteSpace(definition.ZoneId))
                {
                    error = $"Zone entry {index} has an empty ZoneId.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(definition.ScenePath) ||
                    !definition.ScenePath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                {
                    error = $"Zone '{definition.ZoneId}' has no valid .unity scene path.";
                    return false;
                }

                if (!lookup.TryAdd(definition.ZoneId, definition.ScenePath))
                {
                    error = $"Duplicate ZoneId '{definition.ZoneId}'.";
                    return false;
                }
            }

            error = null;
            return true;
        }
    }
}
