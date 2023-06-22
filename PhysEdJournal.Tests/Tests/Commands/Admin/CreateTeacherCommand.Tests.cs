﻿using PhysEdJournal.Core.Entities.Types;
using PhysEdJournal.Core.Exceptions.TeacherExceptions;
using PhysEdJournal.Infrastructure.Commands.AdminCommands;
using PhysEdJournal.Tests.Setup;
using PhysEdJournal.Tests.Setup.Utils;

namespace PhysEdJournal.Tests.Tests.Commands.Admin;

public sealed class CreateTeacherCommandTests : DatabaseTestsHelper
{
    [Fact]
    public async Task CreateTeacherAsync_WhenNewTeacher_ShouldCreateTeacher()
    {
        // Arrange
        await using var context = CreateContext();
        await ClearDatabase(context);
            
        var command = new CreateTeacherCommand(context);
        var payload = new CreateTeacherCommandPayload
        {
            TeacherGuid = "Default",
            FullName = "Default",
            Permissions = TeacherPermissions.DefaultAccess,
            Groups = null
        };

        // Act
        var result = await command.ExecuteAsync(payload);
        var teacherFromDb = await context.Teachers.FindAsync(payload.TeacherGuid);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(teacherFromDb);
        Assert.Equal(payload.TeacherGuid, teacherFromDb.TeacherGuid);
    }
     
    [Fact]
    public async Task CreateTeacherAsync_WhenDuplicateTeacher_ShouldThrow()
    { 
        // Arrange
        await using var context = CreateContext();
        await ClearDatabase(context);
            
        var command = new CreateTeacherCommand(context);
        var teacher = EntitiesFactory.DefaultTeacherEntity(TeacherPermissions.DefaultAccess);
        var payload = new CreateTeacherCommandPayload
        {
            TeacherGuid = teacher.TeacherGuid,
            FullName = teacher.FullName,
            Permissions = teacher.Permissions,
            Groups = null
        };

        await context.Teachers.AddAsync(teacher);
        await context.SaveChangesAsync();

        // Act
        var result = await command.ExecuteAsync(payload);

        // Assert
        Assert.False(result.IsSuccess);
        result.Match(_ => true, exception =>
        {
            Assert.IsType<TeacherAlreadyExistsException>(exception);
            return true;
        });
    }
}