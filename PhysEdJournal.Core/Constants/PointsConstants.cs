﻿namespace PhysEdJournal.Core.Constants;

public static class PointsConstants
{
    public const int MAX_POINTS_FOR_STANDARDS = 30;
    public const int VISIT_LIFE_DAYS = 7;
    public const int POINT_AMOUNT = 50;
    
    public static double CalculateTotalPoints(int visits, double visitValue, int additionalPoints, int pointsForStandards)  
         => visits * visitValue + additionalPoints + pointsForStandards;
}