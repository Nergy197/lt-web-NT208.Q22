using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class SaveRoundTripTests
{
    [Test]
    public void PlayerSave_SurvivesJsonRoundTrip()
    {
        var original = new PlayerSave
        {
            _id = "guest_abc12345_slot_2",
            slotId = 2,
            saveTime = "28/06/2026 10:00:00",
            lastSaveScene = "Chapter5_MapBattle",
            lastSavePointId = "sp_01",
            chapter1TutorialCompleted = true,
            party = new List<UnitSave>
            {
                new UnitSave { entityName = "Hero", level = 3, currentHP = 77, currentExp = 120 },
                new UnitSave { entityName = "Mage", level = 2, currentHP = 40, currentExp = 10 },
            }
        };

        string json = JsonUtility.ToJson(original);
        var restored = JsonUtility.FromJson<PlayerSave>(json);

        Assert.AreEqual(original._id, restored._id);
        Assert.AreEqual(original.slotId, restored.slotId);
        Assert.AreEqual(original.lastSaveScene, restored.lastSaveScene);
        Assert.AreEqual(original.lastSavePointId, restored.lastSavePointId);
        Assert.AreEqual(original.chapter1TutorialCompleted, restored.chapter1TutorialCompleted);
        Assert.AreEqual(2, restored.party.Count);
        Assert.AreEqual("Hero", restored.party[0].entityName);
        Assert.AreEqual(3, restored.party[0].level);
        Assert.AreEqual(77, restored.party[0].currentHP);
        Assert.AreEqual(120, restored.party[0].currentExp);
        Assert.AreEqual("Mage", restored.party[1].entityName);
    }

    [Test]
    public void UnitSave_DefaultsAreZeroValued()
    {
        var json = JsonUtility.ToJson(new UnitSave { entityName = "X" });
        var restored = JsonUtility.FromJson<UnitSave>(json);
        Assert.AreEqual("X", restored.entityName);
        Assert.AreEqual(0, restored.level);
        Assert.AreEqual(0, restored.currentHP);
        Assert.AreEqual(0, restored.currentExp);
    }
}
