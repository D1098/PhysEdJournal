﻿using LanguageExt.Common;
using Microsoft.EntityFrameworkCore;
using PhysEdJournal.Core.Constants;
using PhysEdJournal.Core.Entities.DB;
using PhysEdJournal.Core.Exceptions.StudentExceptions;
using PhysEdJournal.Infrastructure.Database;
using PhysEdJournal.Infrastructure.Models;
using PhysEdJournal.Infrastructure.Services;
using static PhysEdJournal.Core.Constants.PointsConstants;

namespace PhysEdJournal.Infrastructure.Commands;

public sealed class ArchiveStudentCommand
{
    private readonly ApplicationContext _applicationContext;
    private readonly StudentArchiver _studentArchiver;
    
    public ArchiveStudentCommand(ApplicationContext applicationContext)
    {
        _applicationContext = applicationContext;
        _studentArchiver = new StudentArchiver(applicationContext); // Деталь имплементации, поэтому не внедряю через DI
    }

    public async Task<Result<ArchivedStudentEntity>> Execute(string studentGuid, bool isForceMode = false)
    {
        var studentFromDb = await _applicationContext.Students
            .AsNoTracking()
            .Where(s => s.StudentGuid == studentGuid)
            .Select(s => new  {
                s.Group.VisitValue, 
                s.Visits, 
                s.AdditionalPoints, 
                s.PointsForStandards, 
                s.FullName, 
                s.GroupNumber, 
                s.HasDebtFromPreviousSemester, 
                s.ArchivedVisitValue, 
                s.CurrentSemesterName})
            .FirstOrDefaultAsync();

        if (studentFromDb is null)
        {
            return new Result<ArchivedStudentEntity>(new StudentNotFoundException(studentGuid));
        }

        var activeSemesterName = (await _applicationContext.GetActiveSemester()).Name;

        if (studentFromDb.CurrentSemesterName == activeSemesterName)
        {
            return new Result<ArchivedStudentEntity>(new CannotMigrateToNewSemesterException(activeSemesterName));
        }

        var totalPoints = CalculateTotalPoints(studentFromDb.Visits, studentFromDb.VisitValue,
            studentFromDb.AdditionalPoints, studentFromDb.PointsForStandards);

        if (isForceMode || totalPoints > POINT_AMOUNT) // если превысил порог по баллам
        {
            var archiveStudentPayload = new ArchiveStudentPayload
            {
                Visits = studentFromDb.Visits,
                FullName = studentFromDb.FullName,
                GroupNumber = studentFromDb.GroupNumber,
                StudentGuid = studentGuid,
                TotalPoints = totalPoints,
                ActiveSemesterName = activeSemesterName,
                CurrentSemesterName = studentFromDb.CurrentSemesterName,
            };

            var archivedStudent = await _studentArchiver.ArchiveStudentAsync(archiveStudentPayload);
            return new Result<ArchivedStudentEntity>(archivedStudent);
        }
        
        await _applicationContext.Students
            .Where(s => s.StudentGuid == studentGuid)
            .ExecuteUpdateAsync(p => p
                .SetProperty(s => s.HasDebtFromPreviousSemester, true)
                .SetProperty(s => s.ArchivedVisitValue, studentFromDb.VisitValue));
        
        return new Result<ArchivedStudentEntity>(new NotEnoughPointsException(studentGuid, totalPoints));
    }
}