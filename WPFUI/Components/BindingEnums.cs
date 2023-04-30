using System;
using System.Windows.Markup;

namespace WPFUI.Components;

public sealed class BindingEnums : MarkupExtension
{
    public Type EnumType { get; }

    public BindingEnums(Type enumType)
    {
        if (enumType is null || !enumType.IsEnum)
            throw new Exception("EnumType cannot be null and of type Enum");

        EnumType = enumType;
    }

    public override object ProvideValue(IServiceProvider serviceProvider) => Enum.GetValues(EnumType);
}