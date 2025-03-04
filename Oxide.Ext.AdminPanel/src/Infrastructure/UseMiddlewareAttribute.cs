using System;

namespace Oxide.Ext.AdminPanel
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class UseMiddlewareAttribute : Attribute
    {
        public Type[] MiddlewareTypes { get; }

        public UseMiddlewareAttribute(params Type[] middlewareTypes)
        {
            MiddlewareTypes = middlewareTypes;
        }
    }
}
