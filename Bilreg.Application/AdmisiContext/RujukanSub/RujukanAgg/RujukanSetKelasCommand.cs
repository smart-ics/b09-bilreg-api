using Bilreg.Application.AdmisiContext.RujukanSub.KelasRujukanAgg;
using Bilreg.Domain.AdmisiContext.RujukanSub.KelasRujukanAgg;
using Bilreg.Domain.AdmisiContext.RujukanSub.RujukanAgg;
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
public record RujukanSetKelasCommand(string RujukanId, string KelasRujukanId) : IRequest, IRujukanKey, IKelasRujukanKey;

