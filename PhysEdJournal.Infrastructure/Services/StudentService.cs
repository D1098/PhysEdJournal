﻿using LanguageExt;
using LanguageExt.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PhysEdJournal.Application.Services;
using PhysEdJournal.Core.Entities.DB;
using PhysEdJournal.Core.Entities.Types;
using PhysEdJournal.Core.Exceptions.StudentExceptions;
using PhysEdJournal.Infrastructure.Database;
using static PhysEdJournal.Infrastructure.Services.StaticFunctions.StudentServiceFunctions;

namespace PhysEdJournal.Infrastructure.Services;

public sealed class StudentService : IStudentService
{
    private readonly IGroupService _groupService;
    private readonly ApplicationContext _applicationContext;
    private readonly SemesterEntity _currentSemester;
    private readonly string UserInfoServerURL;
    private readonly int POINT_AMOUNT; // Кол-во баллов для получения зачета
    
    public StudentService(ApplicationContext applicationContext, TxtFileConfig fileConfig, IConfiguration configuration, IGroupService groupService)
    {
        _groupService = groupService;
        _applicationContext = applicationContext;
        _currentSemester = SemesterEntity.FromString(fileConfig.ReadTextFromFile()); // Чтобы была возможность выставлять баллы и архивировать студентов на основе текущего семестра
        int.TryParse(configuration["PointBorderForSemester"], out POINT_AMOUNT);
        UserInfoServerURL = configuration["UserInfoServerURL"] ?? throw new Exception("Specify UserinfoServerURL in config");
    }

    public async Task<Result<PointsStudentHistoryEntity>> AddPointsAsync(string studentGuid, string teacherGuid, int pointsAmount, DateOnly date, WorkType workType, string? comment = null)
    {
        var student = await _applicationContext.Students.FindAsync(studentGuid);

        if (student is null)
        {
            return await Task.FromResult(new Result<PointsStudentHistoryEntity>(new StudentNotFound(studentGuid)));
        }
        
        student.AdditionalPoints += pointsAmount;

        var record = new PointsStudentHistoryEntity
        {
            TeacherGuid = teacherGuid, 
            Points = pointsAmount,
            Date = date,
            WorkType = workType,
            StudentGuid = studentGuid,
            Comment = comment,
            SemesterId = _currentSemester.Id
        };

        await _applicationContext.StudentsPointsHistory.AddAsync(record);
        _applicationContext.Students.Update(student);
        await _applicationContext.SaveChangesAsync();

        return record;
    }

    public async Task<Result<VisitsStudentHistoryEntity>> IncreaseVisitsAsync(string studentGuid, DateOnly date, string teacherGuid)
    {
        try
        {
            var student = await _applicationContext.Students.FindAsync(studentGuid);

            if (student is null)
            {
                return await Task.FromResult(new Result<VisitsStudentHistoryEntity>(new Exception($"No student with such guid {studentGuid}")));
            }
        
            student.Visits++;

            var record = new VisitsStudentHistoryEntity
            {
                Date = date,
                StudentGuid = studentGuid,
                TeacherGuid = teacherGuid
            };

            await _applicationContext.StudentsVisitsHistory.AddAsync(record);
            _applicationContext.Students.Update(student);
            await _applicationContext.SaveChangesAsync();

            return record;
        }
        catch (Exception err)
        {
            return new Result<VisitsStudentHistoryEntity>(err);
        }
    }

    public async Task<Result<ArchivedStudentEntity>> ArchiveStudent(string studentGuid, bool isForceMode = false)
    {
        var student = await _applicationContext.Students
            .AsNoTracking()
            .Where(s => s.StudentGuid == studentGuid)
            .Select(s => new {s.Group.VisitValue, s.Visits, s.AdditionalPoints, s.FullName, s.GroupNumber, s.HasDebtFromPreviousSemester, s.ArchivedVisitValue})
            .FirstOrDefaultAsync();

        if (student is null)
        {
            return new Result<ArchivedStudentEntity>(new StudentNotFound(studentGuid));
        }

        if (isForceMode || 
            ((student.Visits * student.VisitValue + student.AdditionalPoints) > POINT_AMOUNT)) // если превысил порог по баллам
        {
            return await Archive(studentGuid, student.FullName, student.GroupNumber, student.Visits, student.VisitValue, student.AdditionalPoints);
        }

        await _applicationContext.Students
            .Where(s => s.StudentGuid == studentGuid)
            .ExecuteUpdateAsync(p => p
                .SetProperty(s => s.HasDebtFromPreviousSemester, true)
                .SetProperty(s => s.ArchivedVisitValue, student.VisitValue));
        return new Result<ArchivedStudentEntity>(new NotEnoughPoints(studentGuid));
    }
    
    private async Task<Result<ArchivedStudentEntity>> Archive(string studentGuid, string fullName, string groupNumber, int visitsAmount, double visitValue, int additionalPoints)
    {
        var archivedStudent = new ArchivedStudentEntity
        {
            StudentGuid = studentGuid,
            FullName = fullName,
            GroupNumber = groupNumber,
            TotalPoints = visitsAmount * visitValue + additionalPoints,
            SemesterId = _currentSemester.Id,
            Visits = visitsAmount
        };

        await _applicationContext.ArchivedStudents.AddAsync(archivedStudent);
        await _applicationContext.SaveChangesAsync();

        await _applicationContext.StudentsVisitsHistory
            .Where(h => h.StudentGuid == studentGuid && h.IsArchived == true)
            .ExecuteDeleteAsync();

        // Заархивировать историю с текущего семестра
        await _applicationContext.StudentsPointsHistory
            .Where(h => h.StudentGuid == studentGuid)
            .ExecuteUpdateAsync(p => p
                .SetProperty(s => s.IsArchived, true));

        await _applicationContext.StudentsVisitsHistory
            .Where(h => h.StudentGuid == studentGuid)
            .ExecuteUpdateAsync(p => p
                .SetProperty(s => s.IsArchived, true));

        await _applicationContext.Students
            .Where(s => s.StudentGuid == studentGuid)
            .ExecuteUpdateAsync(p => p
                .SetProperty(s => s.AdditionalPoints, 0)
                .SetProperty(s => s.Visits, 0)
                .SetProperty(s => s.HasDebtFromPreviousSemester, false)
                .SetProperty(s => s.ArchivedVisitValue, 0));

        return archivedStudent;
    }

    public async Task<Result<Unit>> UpdateStudentInfoAsync()
    {
        await _groupService.UpdateGroupsInfoAsync();
        
        const int batchSize = 250;
        var updateTasks = GetAllStudentsAsync(UserInfoServerURL, pageSize: batchSize)
            .Buffer(batchSize)
            .SelectAwait(async actualStudents => new
            {
                actualStudents = actualStudents.ToDictionary(s => s.Guid), 
                dbStudents = (await GetManyStudentsWithManyKeys(_applicationContext, actualStudents
                    .Select(s => s.Guid)
                    .ToArray()))
                    .ToDictionary(d => d.StudentGuid)
            })
            .Select(s => s.actualStudents.Keys
                .Select(actualStudentGuid => 
                    (
                        s.actualStudents[actualStudentGuid], 
                        s.dbStudents.GetValueOrDefault(actualStudentGuid)
                    )))
            .Select(d => d
                .Select(s => GetUpdatedOrCreatedStudentEntities(s.Item1, s.Item2)))
            .Select(s => CommitChangesToContext(_applicationContext, s.ToList()));

        await foreach (var updateTask in updateTasks)
        {
            await updateTask;
        }

        return Unit.Default;
    }
    
    public async Task<Result<Unit>> CreateStudentAsync(StudentEntity studentEntity)
    {
        try
        {
            await _applicationContext.Students.AddAsync(studentEntity);
            await _applicationContext.SaveChangesAsync();
        
            return Unit.Default;
        }
        catch (Exception err)
        {
            return new Result<Unit>(err);
        }
    }

    public async Task<Result<StudentEntity?>> GetStudentAsync(string guid)
    {
        try
        {
            var student = await _applicationContext.Students.FindAsync(guid);
            return student;
        }
        catch (Exception err)
        {
            return new Result<StudentEntity?>(err);
        }
    }

    public async Task<Result<Unit>> UpdateStudentAsync(StudentEntity updatedStudent)
    {
        try
        {
            var student = await _applicationContext.Students.FindAsync(updatedStudent.StudentGuid);

            student = updatedStudent;

            _applicationContext.Students.Update(student);

            await _applicationContext.SaveChangesAsync();
            
            return Unit.Default;
        }
        catch (Exception err)
        {
            return new Result<Unit>(err);
        }
    }
    public async Task<Result<Unit>> DeleteStudentAsync(string guid)
    {
        try
        {
            await _applicationContext.Students.Where(s => s.StudentGuid == guid).ExecuteDeleteAsync();
            return Unit.Default;
        }
        catch (Exception err)
        {
            return new Result<Unit>(err);
        }
    }
}