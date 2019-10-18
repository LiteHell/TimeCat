﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using TimeCat.Core.Commons;
using TimeCat.Core.Database;
using TimeCat.Core.Database.Models;
using TimeCat.Proto.Commons;

namespace TimeCat.Core
{
    static class Dummies
    {
        private static Dictionary<int, int> totalTimes;
        private static Dictionary<int, List<TimestampRange>> timeRanges;
        private static DateTimeOffset offsetStart;
        private static DateTimeOffset offsetEnd;
        private static bool dummiesGenerated = false;
        private static async Task InsertDummies(TimeCatDB _db, DateTimeOffset start)
        {
            totalTimes = new Dictionary<int, int>();
            timeRanges = new Dictionary<int, List<TimestampRange>>();
            TimeSpan unitSpan = new TimeSpan(0, 0, 0, 1);
            Random rnd = new Random();

            int applicationCount = 30;
            int categoriesCount = 5, subCategoriesCount = 5;

            // 카테고리와 어플리케이션 무작위 생성
            for(int i = 0; i < categoriesCount; i++)
                await _db.InsertAsync(new Category() { Id = i + 1, Name = $"Awesome Category {i}", Color = Color.Aqua});
            for (int i = 0; i < subCategoriesCount; i++)
                await _db.InsertAsync(new Category() { Id = i + categoriesCount + 1, CategoryId = rnd.Next(categoriesCount) + 1 ,Name = $"Awesome Category {i}", Color = Color.Aqua});

            for (int i = 0; i < applicationCount; i++)
                await _db.InsertAsync(new Application() { CategoryId = rnd.Next(categoriesCount) + 1, FullName = $"C:\\TestApp{i}.exe", Id = i + 1, IsProductivity = true, Name = $"Test Application {i}", Icon = "chrome", Version = "1.0" });
            
            int logIndex = 0;
            var now = start;

            // 모든 Application 다 Open
            for (int i = 1; i <= applicationCount; i++)
            {
                await _db.InsertAsync(new Activity() { Id = logIndex++, ApplicationId = i, Action = ActionType.Open, Time = now });
                now += unitSpan;
            }

            List<Activity> activities = new List<Activity>();
            for (int i = 0; i < 180; i++)
            {
                int application = rnd.Next(applicationCount) + 1;
                int active1 = rnd.Next(60 * 60 * 3), idle = rnd.Next(60 * 60 * 1);

                activities.Add(new Activity() { Id = logIndex++, ApplicationId = application, Action = ActionType.Focus, Time = now });
                now += unitSpan;

                DateTimeOffset activeStarts = now;
                for (int j = 0; j < active1; j++)
                {
                    activities.Add(new Activity() { Id = logIndex++, ApplicationId = application, Action = ActionType.Active, Time = now });
                    now += unitSpan;
                }
                activities.Add(new Activity() { Id = logIndex++, ApplicationId = application, Action = ActionType.Idle, Time = now });
                DateTimeOffset activeEnds = now;
                now += unitSpan * idle;
                activities.Add(new Activity() { Id = logIndex++, ApplicationId = application, Action = ActionType.Blur, Time = now });
                now += unitSpan;

                if (!totalTimes.ContainsKey(application))
                {
                    totalTimes[application] = 0;
                }
                if (!timeRanges.ContainsKey(application))
                    timeRanges[application] = new List<TimestampRange>();
                timeRanges[application].Add(new TimestampRange(){Start = Timestamp.FromDateTimeOffset(activeStarts), End = Timestamp.FromDateTimeOffset(activeEnds)});
                totalTimes[application] += active1 == 0 ? 0 : (active1 * unitSpan.Seconds);
            }

            await _db.InsertRangeAsync(activities);

            // 모든 Application 다 Close
            for (int i = 1; i <= applicationCount; i++)
            {
                await _db.InsertAsync(new Activity() { Id = logIndex++, ApplicationId = i, Action = ActionType.Close, Time = now });
                now += unitSpan;
            }
            offsetEnd = now;
        }

        public static async Task Create()
        {
            if (dummiesGenerated)
                return;
            dummiesGenerated = true;

            // initialize database
            TimeCatDB _db = TimeCatDB.Instance;

            // insert dummies
            offsetStart = DateTimeOffset.UtcNow - new TimeSpan(4,0,0,0);
            await InsertDummies(_db, offsetStart);
        }
        public static Dictionary<int, int> TotalUseTimesPerApplications => totalTimes;
        public static Dictionary<int, List<TimestampRange>> TimelinesPerApplications => timeRanges;
        public static DateTimeOffset LogStartsAt => offsetStart;
        public static DateTimeOffset LogEndsAt => offsetEnd;
    }
}
