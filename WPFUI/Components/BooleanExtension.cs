﻿using System;
using System.Windows.Markup;

namespace WPFUI.Components;

public sealed class FalseExtension : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => false;
}

public sealed class TrueExtension : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => true;
}
