using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace TrilobitCS.Tests.Infrastructure;

// Counts DB commands only while a measurement is active. The "Api" collection runs sequentially,
// so a single global window cannot be polluted by other tests.
public class CommandCountingInterceptor : DbCommandInterceptor
{
    private readonly List<string> _commands = new();
    private bool _active;

    public IReadOnlyList<string> Commands
    {
        get { lock (_commands) return _commands.ToList(); }
    }

    public void Start()
    {
        lock (_commands) { _commands.Clear(); _active = true; }
    }

    public IReadOnlyList<string> Stop()
    {
        lock (_commands) { _active = false; return _commands.ToList(); }
    }

    private void Record(DbCommand command)
    {
        lock (_commands) { if (_active) _commands.Add(command.CommandText); }
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    { Record(command); return base.ReaderExecutingAsync(command, eventData, result, cancellationToken); }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    { Record(command); return base.ReaderExecuting(command, eventData, result); }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    { Record(command); return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken); }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
    { Record(command); return base.ScalarExecutingAsync(command, eventData, result, cancellationToken); }
}
