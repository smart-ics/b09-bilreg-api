using Bilreg.Application.AdmisiContext.AntrianFeature;

﻿// using MediatR;
//
// namespace Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;
//
// public record AntrianMapHdrMigrasiCmd() : IRequest;
// public class AntrianMapHdrMigrasiHandler : IRequestHandler<AntrianMapHdrMigrasiCmd>
// {
//     private readonly IAntrianMapRepo _mapHdrRepo;
//
//     public AntrianMapHdrMigrasiHandler(IAntrianMapRepo mapHdrRepo)
//     {
//         _mapHdrRepo = mapHdrRepo;
//     }
//
//     public Task Handle(AntrianMapHdrMigrasiCmd request, CancellationToken cancellationToken)
//     {
//         var tglNow = DateTime.Now;
//         _mapHdrRepo.Migrasi(tglNow);
//         return Task.CompletedTask;
//     }
// }
