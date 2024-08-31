using Bilreg.Application.AdmisiContext.RujukanSub.TipeRujukanAgg;
using Bilreg.Domain.AdmisiContext.RujukanSub.RujukanAgg;
using Bilreg.Domain.AdmisiContext.RujukanSub.TipeRujukanAgg;
using FluentAssertions;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bilreg.Application.AdmisiContext.RujukanSub.RujukanAgg;
public record RujukanSetTipeCommand(string RujukanId, string TipeRujukanId) : IRequest, IRujukanKey, ITipeRujukanKey;


