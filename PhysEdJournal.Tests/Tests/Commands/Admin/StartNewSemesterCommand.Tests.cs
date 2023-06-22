﻿using Microsoft.EntityFrameworkCore;
using PhysEdJournal.Core.Exceptions.SemesterExceptions;
using PhysEdJournal.Infrastructure.Commands.AdminCommands;
using PhysEdJournal.Tests.Setup;

namespace PhysEdJournal.Tests.Tests.Commands.Admin;

public sealed class StartNewSemesterCommandTests : DatabaseTestsHelper
{
    [Fact]
    public async Task StartNewSemesterAsync_ValidName_ShouldCreateNewSemester()
    {
        // Arrange
        var cache =  CreateMemoryCache();
        await using var context = CreateContext(cache);
        await ClearDatabase(context);
        
        var command = new StartNewSemesterCommand(context, cache);
        var validSemesterName = "2022-2023/spring";
        var payload = new StartNewSemesterCommandPayload
        {
            SemesterName = validSemesterName
        };

        // Act
        var result = await command.ExecuteAsync(payload);
        var semester = await context.Semesters.FirstOrDefaultAsync(s => s.Name == validSemesterName);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(semester);
        Assert.Equal(semester.Name, validSemesterName);
    }

    [Fact]
    public async Task StartNewSemesterAsync_InvalidName_ShouldReturnSemesterNameValidationException()
    {
        // Arrange
        var cache =  CreateMemoryCache();
        await using var context = CreateContext(cache);
        await ClearDatabase(context);
        
        var command = new StartNewSemesterCommand(context, cache);
        var invalidSemesterName = "invalid_name";
        var payload = new StartNewSemesterCommandPayload
        {
            SemesterName = invalidSemesterName
        };

        // Act
        var result = await command.ExecuteAsync(payload);
    
        // Assert
        Assert.False(result.IsSuccess);
        result.Match(_ => true, exception =>
        {
            Assert.IsType<SemesterNameValidationException>(exception);
            return true;
        });
    }
}