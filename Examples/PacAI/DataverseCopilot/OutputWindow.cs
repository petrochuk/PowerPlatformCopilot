using bolt.system;
using System;

namespace DataverseCopilot;

internal class OutputWindow : IOutputWindow
{
    public bool IsOutputRedirected => throw new NotImplementedException();

    public void Write(string value)
    {
    }

    public void WriteError(string? value)
    {
    }

    public void WriteLine()
    {
    }

    public void WriteLine(string? value)
    {
    }

    public void WriteResultObject(IOutputObject obj)
    {
    }

    public void WriteWarning(string? value)
    {
    }
}
