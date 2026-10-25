using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisitorManagement.Application.Interfaces;

public interface ICorrelationContext
{
    string CorrelationId { get; }
}