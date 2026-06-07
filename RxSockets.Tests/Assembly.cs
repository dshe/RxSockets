global using System;
global using System.Linq;
global using System.Net;
global using System.Net.Sockets;
global using System.Reactive.Linq;
global using System.Threading;
global using System.Threading.Tasks;
global using Xunit;
global using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Usage", "IDE0130:Namespace does not match folder structure,")]
[assembly: SuppressMessage("Usage", "CA2254:The logging message template should not vary between calls")]
[assembly: SuppressMessage("Usage", "CA1861:Prefer static readonbly fields over constant arrays passed as arguments")]
