using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MatchPoint.Application.Logging;

public interface IAuditLogService
{
    Task WriteAsync(AuditLogEntry entry);
}