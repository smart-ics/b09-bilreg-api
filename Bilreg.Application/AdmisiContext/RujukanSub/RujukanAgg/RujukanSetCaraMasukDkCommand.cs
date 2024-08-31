using Bilreg.Application.AdmisiContext.RujukanSub.CaraMasukDkAgg;
using Bilreg.Domain.AdmisiContext.RujukanSub.CaraMasukDkAgg;
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
public record RujukanSetCaraMasukDkCommand(string RujukanId, string CaraMasukDkId) : IRequest, IRujukanKey, ICaraMasukDkKey;

