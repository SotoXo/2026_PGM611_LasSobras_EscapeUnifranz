using System.Linq;
using EscapeUNIFRANZ.Core;
using EscapeUNIFRANZ.Hazards;
using EscapeUNIFRANZ.Puzzles;
using EscapeUNIFRANZ.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace EscapeUNIFRANZ.Tests.EditMode
{
    public sealed class TechnologicalHorrorTests
    {
        private PuzzleData Load(string id)=>AssetDatabase.LoadAssetAtPath<PuzzleData>("Assets/_Project/Data/Narrative/Puzzles/"+id+".asset");
        [Test] public void FirstCablesOnlyNeedEnergyAndData_AndMistakesPreserveProgress()
        {
            var data=Load("power_entrada"); var model=new PuzzleModel(data);
            Assert.That(model.Total,Is.EqualTo(2));
            Assert.That(model.Connect("IN_PWR","OUT_PWR"),Is.EqualTo(PuzzleSubmission.Accepted));
            Assert.That(model.Connect("IN_DATA","OUT_PWR"),Is.EqualTo(PuzzleSubmission.Rejected));
            Assert.That(model.Connect("OUT_DATA","IN_DATA"),Is.EqualTo(PuzzleSubmission.Rejected));
            Assert.That(model.Progress,Is.EqualTo(1)); Assert.That(model.IsComplete,Is.False);
            Assert.That(model.Connect("IN_DATA","OUT_DATA"),Is.EqualTo(PuzzleSubmission.Completed));
            Assert.That(data.ports.Where(p=>p.output).Select(p=>p.label),Is.Not.EqualTo(data.ports.Where(p=>!p.output).Select(p=>p.label)));
        }
        [Test] public void SecondCablesKeepFourSignalsAndAcceptAnyOrder()
        {
            var model=new PuzzleModel(Load("hardware_routing"));
            Assert.That(model.Connect("IN_PWR","OUT_PWR"),Is.EqualTo(PuzzleSubmission.Accepted));
            model.Connect("IN_DATA","OUT_DATA"); Assert.That(model.Progress,Is.EqualTo(2));
            model.Connect("IN_GND","OUT_GND");
            Assert.That(model.Connect("IN_CTRL","OUT_CTRL"),Is.EqualTo(PuzzleSubmission.Completed));
        }
        [Test] public void EnergyOrderWrongSwitchPreservesValidatedStages()
        {
            var model=new PuzzleModel(Load("simulation_power")); model.Command("POWER"); model.Command("CPU");
            Assert.That(model.Command("ACTUATOR"),Is.EqualTo(PuzzleSubmission.Rejected)); Assert.That(model.Progress,Is.EqualTo(2));
            model.Command("NETWORK"); Assert.That(model.Command("ACTUATOR"),Is.EqualTo(PuzzleSubmission.Completed));
        }
        [Test] public void ProgressionRequiresCCTV_AndAllowsMaintenanceBacktrackingBeforeTechnology()
        {
            var state=new GameState(); state.SetFlag(GameFlagId.PowerEntranceRestored);
            Assert.That(DoorAccessRules.CanOpen(state,new[]{GameFlagId.EntranceCodeAccepted},""),Is.False);
            var code=Load("entrance_code"); Assert.That(code.requiredClueId,Is.EqualTo("cctv_codigo"));
            var energy=Load("simulation_power"); foreach(var item in energy.rewardItems) state.AddItem(item);
            var cabinet=Load("maintenance_hall"); Assert.That(DoorAccessRules.CanOpen(state,cabinet.requiredFlags,cabinet.requiredItem),Is.True);
            foreach(var flag in cabinet.completionFlags) state.SetFlag(flag); foreach(var item in cabinet.rewardItems) state.AddItem(item);
            Assert.That(DoorAccessRules.CanOpen(state,new[]{GameFlagId.MaintenanceAccessRestored},""),Is.True);
            var network=Load("technology_network"); Assert.That(state.HasItem(network.requiredItem),Is.True); Assert.That(network.consumeItem,Is.True);
        }
        [Test] public void EveryMajorRepairHasWrittenAndEnvironmentalEvidence()
        {
            foreach(string id in new[]{"power_entrada","arca_access","hardware_routing","simulation_power","technology_network","software_security","francis_network","francis_shutdown"})
            {
                var d=Load(id); Assert.That(d.requiredClueId,Is.Not.Empty,id); Assert.That(d.prerequisiteClues.Any(c=>c.EndsWith("confirm")),Is.True,id);
            }
        }
        [Test] public void SolidWallsBlockLineOfSight_AndTriggerVolumesDoNot()
        {
            var obj=new GameObject("WallForLOS");
            try
            {
                obj.transform.position=new Vector3(301f,300f); var box=obj.AddComponent<BoxCollider2D>(); Physics2D.SyncTransforms();
                Assert.That(DetectionSensor2D.HasLineOfSight(new Vector2(300f,300f),new Vector2(302f,300f),~0),Is.False);
                box.isTrigger=true; Physics2D.SyncTransforms();
                Assert.That(DetectionSensor2D.HasLineOfSight(new Vector2(300f,300f),new Vector2(302f,300f),~0),Is.True);
            }
            finally { Object.DestroyImmediate(obj); }
        }
    }
}
