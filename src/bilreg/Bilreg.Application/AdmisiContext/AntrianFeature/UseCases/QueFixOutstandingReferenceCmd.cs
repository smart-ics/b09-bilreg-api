using Bilreg.Application.AdmisiContext.AntrianFeature;

﻿using MediatR;

namespace Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;

public record QueFixOutstandingReferenceCmd() : IRequest;

public class QueReplaceReffIdHandler : IRequestHandler<QueFixOutstandingReferenceCmd>
{
    private readonly IAntrianRepo _antrianRepo;

    public QueReplaceReffIdHandler(IAntrianRepo antrianRepo)
    {
        _antrianRepo = antrianRepo;
    }

    public Task Handle(QueFixOutstandingReferenceCmd request, CancellationToken cancellationToken)
    {

        _antrianRepo.FixOutstandingReference();
        
        return Task.CompletedTask;
    }
}
