// Copyright (c) 2019 andrewlock
// Copyright (c) 2025 mapleline25
// Licensed under the MIT license.
// 
// Forked and adapted from StronglyTypedId (andrewlock/StronglyTypedId).
// See: https://github.com/andrewlock/StronglyTypedId/blob/master/src/StronglyTypedIds/DiagnosticInfo.cs.
//
// Summery of changes:
// 1. Change some property types and add the method ToDiagnostic() to fit the usage of this project.

using Microsoft.CodeAnalysis;

namespace StronglyTypedIds;

internal sealed record DiagnosticInfo
{
    public DiagnosticInfo(DiagnosticDescriptor descriptor, Location location, string[]? messageArgs = null)
    {
        Descriptor = descriptor;
        Location = location;
        MessageArgs = messageArgs;
    }

    public DiagnosticDescriptor Descriptor { get; }
    public Location Location { get; }
    public string[]? MessageArgs { get; }

    public Diagnostic ToDiagnostic()
    {
        return Diagnostic.Create(Descriptor, Location, MessageArgs);
    }
}
