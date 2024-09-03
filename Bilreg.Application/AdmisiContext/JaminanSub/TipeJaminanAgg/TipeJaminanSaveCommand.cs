using Bilreg.Application.AdmisiContext.JaminanSub.JaminanAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.JaminanAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.TipeJaminanAgg;
using CommunityToolkit.Diagnostics;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Application.AdmisiContext.JaminanSub.TipeJaminanAgg
{
    public record TipeJaminanSaveCommand(string TipeJaminanId, string TipeJaminanName, string JaminanId)
        : IRequest, ITipeJaminanKey, IJaminanKey;

    public class TipeJaminanSaveCommandHandler : IRequestHandler<TipeJaminanSaveCommand>
    {
        private readonly IJaminanDal _jaminanDal;
        private readonly ITipeJaminanDal _tipeJaminanDal;
        private readonly ITipeJaminanWriter _writer;

        public TipeJaminanSaveCommandHandler(IJaminanDal jaminanDal,
            ITipeJaminanDal tipeJaminanDal,
            ITipeJaminanWriter writer)
        {
            _jaminanDal = jaminanDal;
            _tipeJaminanDal = tipeJaminanDal;
            _writer = writer;
        }

        public Task Handle(TipeJaminanSaveCommand request, CancellationToken cancellationToken)
        {
            //  GUARD
            Guard.IsNotNull(request);
            Guard.IsNotWhiteSpace(request.TipeJaminanId);
            Guard.IsNotWhiteSpace(request.TipeJaminanName);
            Guard.IsNotWhiteSpace(request.JaminanId);
            var jaminan = _jaminanDal.GetData(request)
                ?? throw new KeyNotFoundException($"Jaminan with id {request.JaminanId} not found");

            //  BUILD
            var tipeJaminan = _tipeJaminanDal.GetData(request)
                ?? new TipeJaminanModel(request.TipeJaminanId, request.TipeJaminanName);
            tipeJaminan.SetName(request.TipeJaminanName);
            tipeJaminan.Set(jaminan);
            tipeJaminan.Activate();

            //  WRITE
            _ = _writer.Save(tipeJaminan);
            return Task.CompletedTask;
        }
    }
}
