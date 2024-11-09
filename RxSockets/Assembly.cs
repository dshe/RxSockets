global using System;
global using System.Linq;
global using System.Collections.Generic;
global using System.Net;
global using System.Net.Sockets;
global using System.Threading;
global using System.Threading.Tasks;
global using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

[assembly: CLSCompliant(true)]
[assembly: InternalsVisibleTo("RxSockets.Tests")]

[assembly: SuppressMessage("Usage", "CA1848:Use the LoggerMessage delegates")]
[assembly: SuppressMessage("Usage", "IDE0130:Namespace does not match folder structure,")]
