﻿using LanguageExt;
using LanguageExt.Common;

namespace PhysEdJournal.Application.Services;

public interface IGroupService
{
    public Task<Result<Unit>> AssignCuratorAsync(string callerGuid, string groupName, string teacherGuid);
    
    public Task<Result<Unit>> AssignVisitValueAsync(string callerGuid, string groupName, double newVisitValue);

    public Task<Result<Unit>> UpdateGroupsInfoAsync(string callerGuid);
}