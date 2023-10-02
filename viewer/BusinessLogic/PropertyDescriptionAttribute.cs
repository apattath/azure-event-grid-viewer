using System;

namespace viewer.BusinessLogic;

[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
internal class PropertyDescriptionAttribute : Attribute
{
    public string AIDescription { get; set; }

    public PropertyDescriptionAttribute(string description)
    {
        AIDescription = description;
    }
}
