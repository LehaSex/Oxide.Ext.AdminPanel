using System;

namespace Oxide.Ext.AdminPanel
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class WebSocketExposeAttribute : Attribute
    {
    }
}
