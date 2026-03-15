// ==================== qcbf@qq.com | 2025-09-08 ====================

using FLib;

namespace Nets
{
    public enum ENetCmd
    {
        None,
        Heartbeat,
        Dialog,
        Test,
        Login,
        LoginFinish,
        Reconnect,
        BagSetEquipmentBox,
        BagAddItems,
        BagEquipmentTidy,
        BagEquip,
        BagUnequip,
        BagQuickEquip,
        EquipmentDisassemble,
        AddMailItem,
        MailInfo,
        BattleEnter,
        BattleOver,
        SetSkill,
    }
}
