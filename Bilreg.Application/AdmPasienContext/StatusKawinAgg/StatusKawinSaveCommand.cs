using Bilreg.Domain.AdmPasienContext.StatusKawinAgg;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Application.AdmPasienContext.StatusKawinAgg
{
    public record StatusKawinSaveCommand(string StatusKawinId, string StatusKawin) : IRequest, IStatusKawinKey;

    public class StatusKawinSaveHandler : IRequestHandler<StatusKawinSaveCommand>
    {
        private readonly IStatusKawinWriter _writer;

        public Task Handle(StatusKawinSaveCommand request, CancellationToken cancellationToken)
        {
            //Guard
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.StatusKawinId);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.StatusKawin);

            //  Build
            var statuskawin = StatusKawinModel.Create(request.StatusKawinId, request.StatusKawin);

            // Save
            _writer.Save(statuskawin);
            return Task.CompletedTask;
        }

        
    }

    // Unit test

}
