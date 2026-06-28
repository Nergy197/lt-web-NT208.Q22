using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class SaveServiceTests
{
    const int TestSlot = 9; // slot riêng cho test, dọn sau mỗi lần

    [TearDown]
    public void Cleanup() => SaveService.DeleteLocal(TestSlot);

    [Test]
    public void SaveLocal_ThenLoad_RoundTrips()
    {
        SaveService.DeleteLocal(TestSlot);
        Assert.IsFalse(SaveService.HasLocal(TestSlot));

        var save = new PlayerSave
        {
            _id = "guest_test_slot_9",
            slotId = TestSlot,
            party = new List<UnitSave> { new UnitSave { entityName = "Hero", level = 4, currentHP = 33 } }
        };
        SaveService.SaveLocal(TestSlot, save);

        Assert.IsTrue(SaveService.HasLocal(TestSlot));
        Assert.IsTrue(SaveService.TryLoadLocal(TestSlot, out string json));

        var restored = JsonUtility.FromJson<PlayerSave>(json);
        Assert.AreEqual(TestSlot, restored.slotId);
        Assert.AreEqual("Hero", restored.party[0].entityName);
        Assert.AreEqual(4, restored.party[0].level);
    }

    [Test]
    public void DeleteLocal_RemovesSlot()
    {
        SaveService.SaveLocal(TestSlot, new PlayerSave { slotId = TestSlot, party = new List<UnitSave>() });
        Assert.IsTrue(SaveService.HasLocal(TestSlot));

        SaveService.DeleteLocal(TestSlot);
        Assert.IsFalse(SaveService.HasLocal(TestSlot));
        Assert.IsFalse(SaveService.TryLoadLocal(TestSlot, out _));
    }

    [Test]
    public void LocalKey_IsSlotScoped()
    {
        Assert.AreNotEqual(SaveService.LocalKey(0), SaveService.LocalKey(1));
        Assert.AreEqual("PlayerSave_3", SaveService.LocalKey(3));
    }
}
