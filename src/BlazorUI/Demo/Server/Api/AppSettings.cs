﻿namespace Bit.BlazorUI.Demo.Server.Api;

public class AppSettings
{
    public HealthCheckSettings HealthCheckSettings { get; set; } = default!;

    public string UploadPath { get; set; } = default!;
}

public class HealthCheckSettings
{
    public bool EnableHealthChecks { get; set; }
}
