﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace JJMasterData.Core.FormEvents.Args;

public class FormAfterActionEventArgs : EventArgs
{
    public IDictionary<string, object?> Values { get; set; }

    public string? UrlRedirect { get; set; }

    public FormAfterActionEventArgs()
    {
        Values = new Dictionary<string, object?>();
    }

    public FormAfterActionEventArgs(IDictionary<string, object?>values)
    {
        Values = values;
    }

}
