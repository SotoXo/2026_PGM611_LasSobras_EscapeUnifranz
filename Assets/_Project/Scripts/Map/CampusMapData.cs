using System;
using System.Collections.Generic;
using EscapeUNIFRANZ.Core;
using UnityEngine;

namespace EscapeUNIFRANZ.Map
{
    [Serializable]
    public sealed class CampusMapNodeDefinition
    {
        [SerializeField] private string zoneId;
        [SerializeField] private string displayName;
        [SerializeField] private GameFlagId visitedFlag;
        [SerializeField] private GameFlagId unlockFlag;
        [SerializeField] private GameFlagId revealFlag;

        public CampusMapNodeDefinition(
            string zoneId,
            string displayName,
            GameFlagId visitedFlag,
            GameFlagId unlockFlag = GameFlagId.None,
            GameFlagId revealFlag = GameFlagId.None)
        {
            this.zoneId = zoneId;
            this.displayName = displayName;
            this.visitedFlag = visitedFlag;
            this.unlockFlag = unlockFlag;
            this.revealFlag = revealFlag;
        }

        public string ZoneId => zoneId;
        public string DisplayName => displayName;
        public GameFlagId VisitedFlag => visitedFlag;
        public GameFlagId UnlockFlag => unlockFlag;
        public GameFlagId RevealFlag => revealFlag;
    }

    [Serializable]
    public sealed class CampusMapConnectionDefinition
    {
        [SerializeField] private string fromZoneId;
        [SerializeField] private string toZoneId;

        public string FromZoneId => fromZoneId;
        public string ToZoneId => toZoneId;
    }

    [CreateAssetMenu(menuName = "Escape UNIFRANZ/Campus Map Data", fileName = "CampusMapData")]
    public sealed class CampusMapData : ScriptableObject
    {
        [SerializeField] private List<CampusMapNodeDefinition> nodes = new List<CampusMapNodeDefinition>();
        [SerializeField] private List<CampusMapConnectionDefinition> connections =
            new List<CampusMapConnectionDefinition>();

        public IReadOnlyList<CampusMapNodeDefinition> Nodes => nodes;
        public IReadOnlyList<CampusMapConnectionDefinition> Connections => connections;
    }
}
