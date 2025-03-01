﻿using FreeSql.DataAnnotations;
using FreeSql.Internal.CommonProvider;
using FreeSql.SqlServer;
using FreeSql.Tests.DataContext.SqlServer;
using NetTaste;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Xunit;

namespace FreeSql.Tests.SqlServer
{
    [Collection("SqlServerCollection")]
    public class SqlServerSelectTest
    {

        SqlServerFixture _sqlserverFixture;

        public SqlServerSelectTest(SqlServerFixture sqlserverFixture)
        {
            _sqlserverFixture = sqlserverFixture;
        }

        ISelect<Topic> select => g.sqlserver.Select<Topic>();

        [Table(Name = "tb_topic22")]
        class Topic
        {
            [Column(IsIdentity = true, IsPrimary = true)]
            public int Id { get; set; }
            public int? Clicks { get; set; }
            public int TypeGuid { get; set; }
            public TestTypeInfo Type { get; set; }
            public string Title { get; set; }
            public DateTime CreateTime { get; set; }
        }
        class TestTypeInfo
        {
            [Column(IsIdentity = true)]
            public int Guid { get; set; }
            public int ParentId { get; set; }
            public TestTypeParentInfo Parent { get; set; }
            public string Name { get; set; }
        }
        class TestTypeParentInfo
        {
            public int Id { get; set; }
            public string Name { get; set; }

            public List<TestTypeInfo> Types { get; set; }
        }
        public partial class Song
        {
            [Column(IsIdentity = true)]
            public int Id { get; set; }
            public DateTime? Create_time { get; set; }
            public bool? Is_deleted { get; set; }
            public string Title { get; set; }
            public string Url { get; set; }

            public virtual ICollection<Tag> Tags { get; set; }
        }
        public partial class Song_tag
        {
            public int Song_id { get; set; }
            public virtual Song Song { get; set; }

            public int Tag_id { get; set; }
            public virtual Tag Tag { get; set; }
        }
        public partial class Tag
        {
            [Column(IsIdentity = true)]
            public int Id { get; set; }
            public int? Parent_id { get; set; }
            public virtual Tag Parent { get; set; }

            public decimal? Ddd { get; set; }
            public string Name { get; set; }

            public virtual ICollection<Song> Songs { get; set; }
            public virtual ICollection<Tag> Tags { get; set; }
        }

        [Fact]
        public void NegateLambda()
        {
            var fsql = g.sqlserver;

            var t0 = fsql.Select<Tag>().ToSql(a => new
            {
                Id = -a.Id,
                Ddd2 = -a.Ddd
            });
            Assert.Equal(@"SELECT -(a.[Id]) as1, -(a.[Ddd]) as2 
FROM [Tag] a", t0);

            var t1 = fsql.Select<Tag>().Where(a => -a.Id > -100).ToSql();
            Assert.Equal(@"SELECT a.[Id], a.[Parent_id], a.[Ddd], a.[Name] 
FROM [Tag] a 
WHERE (-(a.[Id]) > -100)", t1);
        }

        [Fact]
        public void AsSelect()
        {
            var fsql = g.sqlserver;

            //OneToOne、ManyToOne
            var t0 = fsql.Select<Tag>().Where(a => a.Parent.Parent.Name == "粤语").ToSql();
            fsql.Select<Tag>().Where(a => a.Parent.Parent.Name == "粤语").First();
            //Assert.Equal(@"", t0);

            //OneToMany
            var t1 = fsql.Select<Tag>().Where(a => a.Tags.AsSelect().Any(t => t.Parent.Id == 10)).ToSql();
            fsql.Select<Tag>().Where(a => a.Tags.AsSelect().Any(t => t.Parent.Id == 10)).First();
            Assert.Equal(@"SELECT a.[Id], a.[Parent_id], a.[Ddd], a.[Name] 
FROM [Tag] a 
WHERE (exists(SELECT TOP 1 1 
    FROM [Tag] t 
    LEFT JOIN [Tag] t__Parent ON t__Parent.[Id] = t.[Parent_id] 
    WHERE (t__Parent.[Id] = 10) AND (t.[Parent_id] = a.[Id])))", t1);
            var t11 = fsql.Select<Tag>().Where(a => a.Tags.Any(t => t.Parent.Id == 10)).ToSql();
            fsql.Select<Tag>().Where(a => a.Tags.Any(t => t.Parent.Id == 10)).First();
            Assert.Equal(@"SELECT a.[Id], a.[Parent_id], a.[Ddd], a.[Name] 
FROM [Tag] a 
WHERE (exists(SELECT TOP 1 1 
    FROM [Tag] t 
    LEFT JOIN [Tag] t__Parent ON t__Parent.[Id] = t.[Parent_id] 
    WHERE (t.[Parent_id] = a.[Id]) AND (t__Parent.[Id] = 10)))", t11);
            var t12 = fsql.Select<Tag>().Where(a => a.Parent.Tags.Any(t => t.Parent.Id == 10)).ToSql();
            fsql.Select<Tag>().Where(a => a.Parent.Tags.Any(t => t.Parent.Id == 10)).First();
            Assert.Equal(@"SELECT a.[Id], a.[Parent_id], a__Parent.[Id] as3, a__Parent.[Parent_id] as4, a__Parent.[Ddd], a__Parent.[Name], a.[Ddd] as7, a.[Name] as8 
FROM [Tag] a 
LEFT JOIN [Tag] a__Parent ON a__Parent.[Id] = a.[Parent_id] 
WHERE (exists(SELECT TOP 1 1 
    FROM [Tag] t 
    LEFT JOIN [Tag] t__Parent ON t__Parent.[Id] = t.[Parent_id] 
    WHERE (t.[Parent_id] = a__Parent.[Id]) AND (t__Parent.[Id] = 10)))", t12);
            var t13 = fsql.Select<Tag>().Where(a => a.Tags.Where(t => t.Parent.Id == 10).Any()).ToSql();
            fsql.Select<Tag>().Where(a => a.Tags.Where(t => t.Parent.Id == 10).Any()).First();
            Assert.Equal(@"SELECT a.[Id], a.[Parent_id], a.[Ddd], a.[Name] 
FROM [Tag] a 
WHERE (exists(SELECT TOP 1 1 
    FROM [Tag] t 
    LEFT JOIN [Tag] t__Parent ON t__Parent.[Id] = t.[Parent_id] 
    WHERE (t.[Parent_id] = a.[Id]) AND (t__Parent.[Id] = 10)))", t13);
            var t14 = fsql.Select<Tag>().Where(a => a.Parent.Tags.Where(t => t.Parent.Id == 10).Any()).ToSql();
            fsql.Select<Tag>().Where(a => a.Parent.Tags.Where(t => t.Parent.Id == 10).Any()).First();
            Assert.Equal(@"SELECT a.[Id], a.[Parent_id], a__Parent.[Id] as3, a__Parent.[Parent_id] as4, a__Parent.[Ddd], a__Parent.[Name], a.[Ddd] as7, a.[Name] as8 
FROM [Tag] a 
LEFT JOIN [Tag] a__Parent ON a__Parent.[Id] = a.[Parent_id] 
WHERE (exists(SELECT TOP 1 1 
    FROM [Tag] t 
    LEFT JOIN [Tag] t__Parent ON t__Parent.[Id] = t.[Parent_id] 
    WHERE (t.[Parent_id] = a__Parent.[Id]) AND (t__Parent.[Id] = 10)))", t14);
            var t15 = fsql.Select<Tag>().Where(a => a.Parent.Tags.Where(t => t.Parent.Id == 10).Select(t => t.Name).ToList().Contains(a.Name)).ToSql();
            fsql.Select<Tag>().Where(a => a.Parent.Tags.Where(t => t.Parent.Id == 10).Select(t => t.Name).ToList().Contains(a.Name)).First();
            Assert.Equal(@"SELECT a.[Id], a.[Parent_id], a__Parent.[Id] as3, a__Parent.[Parent_id] as4, a__Parent.[Ddd], a__Parent.[Name], a.[Ddd] as7, a.[Name] as8 
FROM [Tag] a 
LEFT JOIN [Tag] a__Parent ON a__Parent.[Id] = a.[Parent_id] 
WHERE (((a.[Name]) in (SELECT t.[Name] as1 
    FROM [Tag] t 
    LEFT JOIN [Tag] t__Parent ON t__Parent.[Id] = t.[Parent_id] 
    WHERE (t.[Parent_id] = a__Parent.[Id]) AND (t__Parent.[Id] = 10))))", t15);


            //ManyToMany
            var t2 = fsql.Select<Song>().Where(s => s.Tags.AsSelect().Any(t => t.Name == "国语")).ToSql();
            fsql.Select<Song>().Where(s => s.Tags.AsSelect().Any(t => t.Name == "国语")).First();
            Assert.Equal(@"SELECT a.[Id], a.[Create_time], a.[Is_deleted], a.[Title], a.[Url] 
FROM [Song] a 
WHERE (exists(SELECT TOP 1 1 
    FROM [Song_tag] Mt_Ms 
    WHERE (Mt_Ms.[Song_id] = a.[Id]) AND (exists(SELECT TOP 1 1 
        FROM [Tag] t 
        WHERE (t.[Name] = N'国语') AND (t.[Id] = Mt_Ms.[Tag_id])))))", t2);
            var t21 = fsql.Select<Song>().Where(s => s.Tags.Any(t => t.Name == "国语")).ToSql();
            fsql.Select<Song>().Where(s => s.Tags.Any(t => t.Name == "国语")).First();
            Assert.Equal(@"SELECT a.[Id], a.[Create_time], a.[Is_deleted], a.[Title], a.[Url] 
FROM [Song] a 
WHERE (exists(SELECT TOP 1 1 
    FROM [Tag] t 
    WHERE (exists(SELECT TOP 1 1 
        FROM [Song_tag] Mt_Ma 
        WHERE (Mt_Ma.[Tag_id] = t.[Id]) AND (Mt_Ma.[Song_id] = a.[Id]))) AND (t.[Name] = N'国语')))", t21);
            var t22 = fsql.Select<Song>().Where(s => s.Tags.Where(t => t.Name == "国语").Any()).ToSql();
            fsql.Select<Song>().Where(s => s.Tags.Where(t => t.Name == "国语").Any()).First();
            Assert.Equal(@"SELECT a.[Id], a.[Create_time], a.[Is_deleted], a.[Title], a.[Url] 
FROM [Song] a 
WHERE (exists(SELECT TOP 1 1 
    FROM [Tag] t 
    WHERE (exists(SELECT TOP 1 1 
        FROM [Song_tag] Mt_Ma 
        WHERE (Mt_Ma.[Tag_id] = t.[Id]) AND (Mt_Ma.[Song_id] = a.[Id]))) AND (t.[Name] = N'国语')))", t22);
            var t23 = fsql.Select<Tag>().Where(t => t.Parent.Songs.Any(s => s.Title == "中国人")).ToSql();
            fsql.Select<Tag>().Where(t => t.Parent.Songs.Any(s => s.Title == "中国人")).First();
            Assert.Equal(@"SELECT a.[Id], a.[Parent_id], a__Parent.[Id] as3, a__Parent.[Parent_id] as4, a__Parent.[Ddd], a__Parent.[Name], a.[Ddd] as7, a.[Name] as8 
FROM [Tag] a 
LEFT JOIN [Tag] a__Parent ON a__Parent.[Id] = a.[Parent_id] 
WHERE (exists(SELECT TOP 1 1 
    FROM [Song] s 
    WHERE (exists(SELECT TOP 1 1 
        FROM [Song_tag] Ms_Ma__Parent 
        WHERE (Ms_Ma__Parent.[Song_id] = s.[Id]) AND (Ms_Ma__Parent.[Tag_id] = a__Parent.[Id]))) AND (s.[Title] = N'中国人')))", t23);
            var t24 = fsql.Select<Tag>().Where(t => t.Parent.Songs.Where(s => s.Title == "中国人").Any()).ToSql();
            fsql.Select<Tag>().Where(t => t.Parent.Songs.Where(s => s.Title == "中国人").Any()).First();
            Assert.Equal(@"SELECT a.[Id], a.[Parent_id], a__Parent.[Id] as3, a__Parent.[Parent_id] as4, a__Parent.[Ddd], a__Parent.[Name], a.[Ddd] as7, a.[Name] as8 
FROM [Tag] a 
LEFT JOIN [Tag] a__Parent ON a__Parent.[Id] = a.[Parent_id] 
WHERE (exists(SELECT TOP 1 1 
    FROM [Song] s 
    WHERE (exists(SELECT TOP 1 1 
        FROM [Song_tag] Ms_Ma__Parent 
        WHERE (Ms_Ma__Parent.[Song_id] = s.[Id]) AND (Ms_Ma__Parent.[Tag_id] = a__Parent.[Id]))) AND (s.[Title] = N'中国人')))", t24);
            var t25 = fsql.Select<Tag>().Where(t => t.Parent.Songs.Where(s => s.Title == "中国人").Select(s => s.Title).ToList().Contains(t.Name)).ToSql();
            fsql.Select<Tag>().Where(t => t.Parent.Songs.Where(s => s.Title == "中国人").Select(s => s.Title).ToList().Contains(t.Name)).First();
            Assert.Equal(@"SELECT a.[Id], a.[Parent_id], a__Parent.[Id] as3, a__Parent.[Parent_id] as4, a__Parent.[Ddd], a__Parent.[Name], a.[Ddd] as7, a.[Name] as8 
FROM [Tag] a 
LEFT JOIN [Tag] a__Parent ON a__Parent.[Id] = a.[Parent_id] 
WHERE (((a.[Name]) in (SELECT s.[Title] as1 
    FROM [Song] s 
    WHERE (exists(SELECT TOP 1 1 
        FROM [Song_tag] Ms_Ma__Parent 
        WHERE (Ms_Ma__Parent.[Song_id] = s.[Id]) AND (Ms_Ma__Parent.[Tag_id] = a__Parent.[Id]))) AND (s.[Title] = N'中国人'))))", t25);


            var t3 = fsql.Select<Song>().ToList(r => new
            {
                r.Title,
                c2 = r.Tags.Count,
                c3 = r.Tags.Count(),
                c4 = r.Tags.Count(tag => tag.Id > 0),
                s1 = r.Tags.Sum(b => b.Id + 0),
                a1 = r.Tags.Average(b => b.Id + 1),
                m1 = r.Tags.Max(b => b.Id + 2),
                m2 = r.Tags.Min(b => b.Id + 3),
                f1 = r.Tags.Select(b => b.Name).First(),

                count = r.Tags.AsSelect().Count(),
                sum = r.Tags.AsSelect().Sum(b => b.Id + 0),
                avg = r.Tags.AsSelect().Avg(b => b.Id + 1),
                max = r.Tags.AsSelect().Max(b => b.Id + 2),
                min = r.Tags.AsSelect().Min(b => b.Id + 3),
                first = r.Tags.AsSelect().First(b => b.Name)
            });
        }

        [Fact]
        public void Lazy()
        {
            var tags = g.sqlserver.Select<Tag>().Where(a => a.Parent.Name == "xxx")
                .LeftJoin(a => a.Parent_id == a.Parent.Id)
                .ToSql();

            var songs = g.sqlserver.Select<Song>().Limit(10).ToList();


        }
        [Fact]
        public void ToDataTable()
        {
            var items = new List<Topic>();
            for (var a = 0; a < 10; a++) items.Add(new Topic { Id = a + 1, Title = $"newtitle{a}", Clicks = a * 100, CreateTime = DateTime.Now });

            Assert.Single(g.sqlserver.Insert<Topic>().AppendData(items.First()).ExecuteInserted());
            Assert.Equal(10, g.sqlserver.Insert<Topic>().AppendData(items).ExecuteInserted().Count);

            //items = Enumerable.Range(0, 9989).Select(a => new Topic { Title = "newtitle" + a, CreateTime = DateTime.Now }).ToList();
            //;
            //Assert.Equal(9989, g.sqlserver.Insert<Topic>(items).NoneParameter().ExecuteAffrows());

            var dt1 = select.Limit(10).ToDataTable();
            var dt2 = select.Limit(10).ToDataTable("Id, getdate()");
            var dt3 = select.Limit(10).ToDataTable(a => new { a.Id, a.Type.Name, now = DateTime.Now });
        }
        class TestDto
        {
            public int id { get; set; }
            public string name { get; set; } //这是join表的属性
            public int ParentId { get; set; } //这是join表的属性
        }
        class TestDto2
        {
            public int id { get; set; }
            public string name { get; set; } //这是join表的属性
            public int ParentId { get; set; } //这是join表的属性

            public TestDto2() { }
            public TestDto2(int id, string name)
            {
                this.id = id;
                this.name = name;
            }
        }
        [Fact]
        public void ToList()
        {
            var testDto1 = select.Limit(10).ToList(a => new TestDto { id = a.Id, name = a.Title });
            var testDto2 = select.Limit(10).ToList(a => new TestDto());
            var testDto3 = select.Limit(10).ToList(a => new TestDto { });
            var testDto4 = select.Limit(10).ToList(a => new TestDto() { });

            var testDto11 = select.LeftJoin(a => a.Type.Guid == a.TypeGuid).Limit(10).ToList(a => new TestDto { id = a.Id, name = a.Title });
            var testDto22 = select.LeftJoin(a => a.Type.Guid == a.TypeGuid).Limit(10).ToList(a => new TestDto());
            var testDto33 = select.LeftJoin(a => a.Type.Guid == a.TypeGuid).Limit(10).ToList(a => new TestDto { });
            var testDto44 = select.LeftJoin(a => a.Type.Guid == a.TypeGuid).Limit(10).ToList(a => new TestDto() { });

            var testDto211 = select.Limit(10).ToList(a => new TestDto2(a.Id, a.Title));
            var testDto212 = select.Limit(10).ToList(a => new TestDto2());
            var testDto213 = select.Limit(10).ToList(a => new TestDto2 { });
            var testDto214 = select.Limit(10).ToList(a => new TestDto2() { });
            var testDto215 = select.Limit(10).ToList<TestDto2>();

            var testDto2211 = select.LeftJoin(a => a.Type.Guid == a.TypeGuid).Limit(10).ToList(a => new TestDto2(a.Id, a.Title));
            var testDto2222 = select.LeftJoin(a => a.Type.Guid == a.TypeGuid).Limit(10).ToList(a => new TestDto2());
            var testDto2233 = select.LeftJoin(a => a.Type.Guid == a.TypeGuid).Limit(10).ToList(a => new TestDto2 { });
            var testDto2244 = select.LeftJoin(a => a.Type.Guid == a.TypeGuid).Limit(10).ToList(a => new TestDto2() { });
            var testDto2255 = select.LeftJoin(a => a.Type.Guid == a.TypeGuid).Limit(10).ToList<TestDto2>();

            g.sqlserver.Insert<TestGuidIdToList>().AppendData(new TestGuidIdToList()).ExecuteAffrows();
            var testGuidId5 = g.sqlserver.Select<TestGuidIdToList>().ToList();
            var testGuidId6 = g.sqlserver.Select<TestGuidIdToList>().ToList(a => a.id);

            var t11 = select.Where(a => a.Type.Name.Length > 0).ToList(true);
            var t21 = select.Where(a => a.Type.Parent.Name.Length > 0).ToList(true);

            g.sqlserver.Delete<District>().Where("1=1").ExecuteAffrows();
            var repo = g.sqlserver.GetRepository<District>();
            repo.DbContextOptions.EnableCascadeSave = true;
            repo.Insert(new District
            {
                Code = "001",
                Name = "001_name",
                Childs = new List<District>(new[] {
                    new District{
                        Code = "001_01",
                        Name = "001_01_name"
                    },
                    new District{
                        Code = "001_02",
                        Name = "001_02_name"
                    }
                })
            });
            var ddd = g.sqlserver.Select<District>().LeftJoin(d => d.ParentCode == d.Parent.Code).ToTreeList();
            Assert.Single(ddd);
            Assert.Equal(2, ddd[0].Childs.Count);
        }
        public class District
        {
            [Column(IsPrimary = true, StringLength = 6)]
            public string Code { get; set; }

            [Column(StringLength = 20, IsNullable = false)]
            public string Name { get; set; }

            [Column(StringLength = 6)]
            public string ParentCode { get; set; }

            [Navigate(nameof(ParentCode))]
            public District Parent { get; set; }

            [Navigate(nameof(ParentCode))]
            public List<District> Childs { get; set; }
        }
        [Fact]
        public void ToDictionary()
        {
            g.sqlserver.Insert(new Topic { Title = "xxx" }).ExecuteAffrows();
            var testDto1 = select.Limit(10).ToDictionary(a => a.Id);
            var testDto2 = select.Limit(10).ToDictionary(a => a.Id, a => new { a.Id, a.Title });

            var repo = g.sqlserver.GetRepository<Topic>();
            var dic = repo.Select.Limit(10).ToDictionary(a => a.Id);
            var first = dic.First().Value;
            first.Clicks++;
            repo.Update(first);
        }
        class TestGuidIdToList
        {
            public Guid id { get; set; }
            public string title { get; set; } = Guid.NewGuid().ToString();
        }
        [Fact]
        public void ToOne()
        {
            var testnotfind = select.Where("1=2").First(a => a.CreateTime);
            Assert.Equal(default(DateTime), testnotfind);
        }
        [Fact]
        public void ToSql()
        {
        }
        [Fact]
        public void Any()
        {
            var count = select.Where(a => 1 == 1).Count();
            Assert.False(select.Where(a => 1 == 2).Any());
            Assert.Equal(count > 0, select.Where(a => 1 == 1).Any());

            var sql2222 = select.Where(a =>
                select.Where(b => b.Id == a.Id &&
                    select.Where(c => c.Id == b.Id).Where(d => d.Id == a.Id).Where(e => e.Id == b.Id)
                    //.Offset(a.Id)
                    .Any()
                ).Any(c => c.Id == a.Id + 10)
            );
            var sql2222Tolist = sql2222.ToList();

            var collectionSelect = select.Where(a =>
                a.Type.Guid == a.TypeGuid &&
                a.Type.Parent.Id == a.Type.ParentId &&
                a.Type.Parent.Types.AsSelect().Where(b => b.Name == a.Title).Any(b => b.ParentId == a.Type.Parent.Id)
            );
            collectionSelect.ToList();
        }
        [Fact]
        public void Count()
        {
            var count = select.Where(a => 1 == 1).Count();
            var count11 = select.Where(a => 1 == 1).OrderBy(a => a.Id).Count();
            select.Where(a => 1 == 1).Count(out var count2);
            select.Where(a => 1 == 1).OrderBy(a => a.Id).Count(out var count22);
            Assert.Equal(count, count2);
            Assert.Equal(count11, count22);
            Assert.Equal(0, select.Where(a => 1 == 2).Count());

            var subquery = select.ToSql(a => new
            {
                all = a,
                count = select.Where(b => b.Id > 0 && b.Id == a.Id).Count()
            });
            var subqueryList = select.ToList(a => new
            {
                all = a,
                count = select.Where(b => b.Id > 0 && b.Id == a.Id).Count()
            });
        }
        [Fact]
        public void Master()
        {
            Assert.StartsWith(" SELECT", select.Master().Where(a => 1 == 1).ToSql());
        }

        [Fact]
        public void From()
        {
            var query2 = select.From<TestTypeInfo>((s, b) => s
                 .LeftJoin(a => a.TypeGuid == b.Guid)
                );
            var sql2 = query2.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], b.[Guid], b.[ParentId], b.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22] a LEFT JOIN [TestTypeInfo] b ON a.[TypeGuid] = b.[Guid]", sql2);
            query2.ToList();

            var query3 = select.From<TestTypeInfo, TestTypeParentInfo>((s, b, c) => s
                 .LeftJoin(a => a.TypeGuid == b.Guid)
                 .LeftJoin(a => b.ParentId == c.Id)
                );
            var sql3 = query3.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], b.[Guid], b.[ParentId], b.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22] a LEFT JOIN [TestTypeInfo] b ON a.[TypeGuid] = b.[Guid] LEFT JOIN [TestTypeParentInfo] c ON b.[ParentId] = c.[Id]", sql3);
            query3.ToList();
        }
        [Fact]
        public void LeftJoin()
        {
            //����е�������a.Type��a.Type.Parent ���ǵ�������
            var query = select.LeftJoin(a => a.Type.Guid == a.TypeGuid);
            var sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a__Type.[Guid], a__Type.[ParentId], a__Type.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22] a LEFT JOIN [TestTypeInfo] a__Type ON a__Type.[Guid] = a.[TypeGuid]", sql);
            query.ToList();

            query = select.LeftJoin(a => a.Type.Guid == a.TypeGuid && a.Type.Name == "xxx");
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a__Type.[Guid], a__Type.[ParentId], a__Type.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22] a LEFT JOIN [TestTypeInfo] a__Type ON a__Type.[Guid] = a.[TypeGuid] AND a__Type.[Name] = N'xxx'", sql);
            query.ToList();

            query = select.LeftJoin(a => a.Type.Guid == a.TypeGuid && a.Type.Name == "xxx").Where(a => a.Type.Parent.Id == 10);
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a__Type.[Guid], a__Type.[ParentId], a__Type.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22] a LEFT JOIN [TestTypeInfo] a__Type ON a__Type.[Guid] = a.[TypeGuid] AND a__Type.[Name] = N'xxx' LEFT JOIN [TestTypeParentInfo] a__Type__Parent ON a__Type__Parent.[Id] = a__Type.[ParentId] WHERE (a__Type__Parent.[Id] = 10)", sql);
            query.ToList();

            //���û�е�������
            query = select.LeftJoin<TestTypeInfo>((a, b) => b.Guid == a.TypeGuid);
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], b.[Guid], b.[ParentId], b.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22] a LEFT JOIN [TestTypeInfo] b ON b.[Guid] = a.[TypeGuid]", sql);
            query.ToList();

            query = select.LeftJoin<TestTypeInfo>((a, b) => b.Guid == a.TypeGuid && b.Name == "xxx");
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], b.[Guid], b.[ParentId], b.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22] a LEFT JOIN [TestTypeInfo] b ON b.[Guid] = a.[TypeGuid] AND b.[Name] = N'xxx'", sql);
            query.ToList();

            query = select.LeftJoin<TestTypeInfo>((a, b) => b.Guid == a.TypeGuid && b.Name == "xxx").Where(a => a.Type.Parent.Id == 10);
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a__Type.[Guid], a__Type.[ParentId], a__Type.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22] a LEFT JOIN [TestTypeInfo] b ON b.[Guid] = a.[TypeGuid] AND b.[Name] = N'xxx' LEFT JOIN [TestTypeInfo] a__Type ON a__Type.[Guid] = a.[TypeGuid] LEFT JOIN [TestTypeParentInfo] a__Type__Parent ON a__Type__Parent.[Id] = a__Type.[ParentId] WHERE (a__Type__Parent.[Id] = 10)", sql);
            query.ToList();

            //�������
            query = select
                .LeftJoin(a => a.Type.Guid == a.TypeGuid)
                .LeftJoin(a => a.Type.Parent.Id == a.Type.ParentId);
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a__Type.[Guid], a__Type.[ParentId], a__Type.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22] a LEFT JOIN [TestTypeInfo] a__Type ON a__Type.[Guid] = a.[TypeGuid] LEFT JOIN [TestTypeParentInfo] a__Type__Parent ON a__Type__Parent.[Id] = a__Type.[ParentId]", sql);
            query.ToList();

            query = select
                .LeftJoin<TestTypeInfo>((a, a__Type) => a__Type.Guid == a.TypeGuid)
                .LeftJoin<TestTypeParentInfo>((a, c) => c.Id == a.Type.ParentId);
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a__Type.[Guid], a__Type.[ParentId], a__Type.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22] a LEFT JOIN [TestTypeInfo] a__Type ON a__Type.[Guid] = a.[TypeGuid] LEFT JOIN [TestTypeParentInfo] c ON c.[Id] = a__Type.[ParentId]", sql);
            query.ToList();

            //���û�е�������b��c������ϵ
            var query2 = select.From<TestTypeInfo, TestTypeParentInfo>((s, b, c) => s
                 .LeftJoin(a => a.TypeGuid == b.Guid)
                 .LeftJoin(a => b.ParentId == c.Id));
            sql = query2.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], b.[Guid], b.[ParentId], b.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22] a LEFT JOIN [TestTypeInfo] b ON a.[TypeGuid] = b.[Guid] LEFT JOIN [TestTypeParentInfo] c ON b.[ParentId] = c.[Id]", sql);
            query2.ToList();

            //������϶����㲻��
            query = select.LeftJoin("TestTypeInfo b on b.Guid = a.TypeGuid");
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime] FROM [tb_topic22] a LEFT JOIN TestTypeInfo b on b.Guid = a.TypeGuid", sql);
            query.ToList();

            query = select.LeftJoin("TestTypeInfo b on b.Guid = a.TypeGuid and b.Name = @bname", new { bname = "xxx" });
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime] FROM [tb_topic22] a LEFT JOIN TestTypeInfo b on b.Guid = a.TypeGuid and b.Name = @bname", sql);
            query.ToList();
        }
        [Fact]
        public void InnerJoin()
        {
            //����е�������a.Type��a.Type.Parent ���ǵ�������
            var query = select.InnerJoin(a => a.Type.Guid == a.TypeGuid);
            var sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a__Type.[Guid], a__Type.[ParentId], a__Type.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22] a INNER JOIN [TestTypeInfo] a__Type ON a__Type.[Guid] = a.[TypeGuid]", sql);
            query.ToList();

            query = select.InnerJoin(a => a.Type.Guid == a.TypeGuid && a.Type.Name == "xxx");
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a__Type.[Guid], a__Type.[ParentId], a__Type.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22] a INNER JOIN [TestTypeInfo] a__Type ON a__Type.[Guid] = a.[TypeGuid] AND a__Type.[Name] = N'xxx'", sql);
            query.ToList();

            query = select.InnerJoin(a => a.Type.Guid == a.TypeGuid && a.Type.Name == "xxx").Where(a => a.Type.Parent.Id == 10);
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a__Type.[Guid], a__Type.[ParentId], a__Type.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22] a INNER JOIN [TestTypeInfo] a__Type ON a__Type.[Guid] = a.[TypeGuid] AND a__Type.[Name] = N'xxx' LEFT JOIN [TestTypeParentInfo] a__Type__Parent ON a__Type__Parent.[Id] = a__Type.[ParentId] WHERE (a__Type__Parent.[Id] = 10)", sql);
            query.ToList();

            //���û�е�������
            query = select.InnerJoin<TestTypeInfo>((a, b) => b.Guid == a.TypeGuid);
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], b.[Guid], b.[ParentId], b.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22] a INNER JOIN [TestTypeInfo] b ON b.[Guid] = a.[TypeGuid]", sql);
            query.ToList();

            query = select.InnerJoin<TestTypeInfo>((a, b) => b.Guid == a.TypeGuid && b.Name == "xxx");
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], b.[Guid], b.[ParentId], b.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22] a INNER JOIN [TestTypeInfo] b ON b.[Guid] = a.[TypeGuid] AND b.[Name] = N'xxx'", sql);
            query.ToList();

            query = select.InnerJoin<TestTypeInfo>((a, b) => b.Guid == a.TypeGuid && b.Name == "xxx").Where(a => a.Type.Parent.Id == 10);
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a__Type.[Guid], a__Type.[ParentId], a__Type.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22] a INNER JOIN [TestTypeInfo] b ON b.[Guid] = a.[TypeGuid] AND b.[Name] = N'xxx' LEFT JOIN [TestTypeInfo] a__Type ON a__Type.[Guid] = a.[TypeGuid] LEFT JOIN [TestTypeParentInfo] a__Type__Parent ON a__Type__Parent.[Id] = a__Type.[ParentId] WHERE (a__Type__Parent.[Id] = 10)", sql);
            query.ToList();

            //�������
            query = select
                .InnerJoin(a => a.Type.Guid == a.TypeGuid)
                .InnerJoin(a => a.Type.Parent.Id == a.Type.ParentId);
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a__Type.[Guid], a__Type.[ParentId], a__Type.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22] a INNER JOIN [TestTypeInfo] a__Type ON a__Type.[Guid] = a.[TypeGuid] INNER JOIN [TestTypeParentInfo] a__Type__Parent ON a__Type__Parent.[Id] = a__Type.[ParentId]", sql);
            query.ToList();

            query = select
                .InnerJoin<TestTypeInfo>((a, a__Type) => a__Type.Guid == a.TypeGuid)
                .InnerJoin<TestTypeParentInfo>((a, c) => c.Id == a.Type.ParentId);
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a__Type.[Guid], a__Type.[ParentId], a__Type.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22] a INNER JOIN [TestTypeInfo] a__Type ON a__Type.[Guid] = a.[TypeGuid] INNER JOIN [TestTypeParentInfo] c ON c.[Id] = a__Type.[ParentId]", sql);
            query.ToList();

            //���û�е�������b��c������ϵ
            var query2 = select.From<TestTypeInfo, TestTypeParentInfo>((s, b, c) => s
                 .InnerJoin(a => a.TypeGuid == b.Guid)
                 .InnerJoin(a => b.ParentId == c.Id));
            sql = query2.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], b.[Guid], b.[ParentId], b.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22] a INNER JOIN [TestTypeInfo] b ON a.[TypeGuid] = b.[Guid] INNER JOIN [TestTypeParentInfo] c ON b.[ParentId] = c.[Id]", sql);
            query2.ToList();

            //������϶����㲻��
            query = select.InnerJoin("TestTypeInfo b on b.Guid = a.TypeGuid");
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime] FROM [tb_topic22] a INNER JOIN TestTypeInfo b on b.Guid = a.TypeGuid", sql);
            query.ToList();

            query = select.InnerJoin("TestTypeInfo b on b.Guid = a.TypeGuid and b.Name = @bname", new { bname = "xxx" });
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime] FROM [tb_topic22] a INNER JOIN TestTypeInfo b on b.Guid = a.TypeGuid and b.Name = @bname", sql);
            query.ToList();

        }
        [Fact]
        public void RightJoin()
        {
            //����е�������a.Type��a.Type.Parent ���ǵ�������
            var query = select.RightJoin(a => a.Type.Guid == a.TypeGuid);
            var sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a__Type.[Guid], a__Type.[ParentId], a__Type.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22] a RIGHT JOIN [TestTypeInfo] a__Type ON a__Type.[Guid] = a.[TypeGuid]", sql);
            query.ToList();

            query = select.RightJoin(a => a.Type.Guid == a.TypeGuid && a.Type.Name == "xxx");
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a__Type.[Guid], a__Type.[ParentId], a__Type.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22] a RIGHT JOIN [TestTypeInfo] a__Type ON a__Type.[Guid] = a.[TypeGuid] AND a__Type.[Name] = N'xxx'", sql);
            query.ToList();

            query = select.RightJoin(a => a.Type.Guid == a.TypeGuid && a.Type.Name == "xxx").Where(a => a.Type.Parent.Id == 10);
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a__Type.[Guid], a__Type.[ParentId], a__Type.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22] a RIGHT JOIN [TestTypeInfo] a__Type ON a__Type.[Guid] = a.[TypeGuid] AND a__Type.[Name] = N'xxx' LEFT JOIN [TestTypeParentInfo] a__Type__Parent ON a__Type__Parent.[Id] = a__Type.[ParentId] WHERE (a__Type__Parent.[Id] = 10)", sql);
            query.ToList();

            //���û�е�������
            query = select.RightJoin<TestTypeInfo>((a, b) => b.Guid == a.TypeGuid);
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], b.[Guid], b.[ParentId], b.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22] a RIGHT JOIN [TestTypeInfo] b ON b.[Guid] = a.[TypeGuid]", sql);
            query.ToList();

            query = select.RightJoin<TestTypeInfo>((a, b) => b.Guid == a.TypeGuid && b.Name == "xxx");
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], b.[Guid], b.[ParentId], b.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22] a RIGHT JOIN [TestTypeInfo] b ON b.[Guid] = a.[TypeGuid] AND b.[Name] = N'xxx'", sql);
            query.ToList();

            query = select.RightJoin<TestTypeInfo>((a, b) => b.Guid == a.TypeGuid && b.Name == "xxx").Where(a => a.Type.Parent.Id == 10);
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a__Type.[Guid], a__Type.[ParentId], a__Type.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22] a RIGHT JOIN [TestTypeInfo] b ON b.[Guid] = a.[TypeGuid] AND b.[Name] = N'xxx' LEFT JOIN [TestTypeInfo] a__Type ON a__Type.[Guid] = a.[TypeGuid] LEFT JOIN [TestTypeParentInfo] a__Type__Parent ON a__Type__Parent.[Id] = a__Type.[ParentId] WHERE (a__Type__Parent.[Id] = 10)", sql);
            query.ToList();

            //�������
            query = select
                .RightJoin(a => a.Type.Guid == a.TypeGuid)
                .RightJoin(a => a.Type.Parent.Id == a.Type.ParentId);
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a__Type.[Guid], a__Type.[ParentId], a__Type.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22] a RIGHT JOIN [TestTypeInfo] a__Type ON a__Type.[Guid] = a.[TypeGuid] RIGHT JOIN [TestTypeParentInfo] a__Type__Parent ON a__Type__Parent.[Id] = a__Type.[ParentId]", sql);
            query.ToList();

            query = select
                .RightJoin<TestTypeInfo>((a, a__Type) => a__Type.Guid == a.TypeGuid)
                .RightJoin<TestTypeParentInfo>((a, c) => c.Id == a.Type.ParentId);
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a__Type.[Guid], a__Type.[ParentId], a__Type.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22] a RIGHT JOIN [TestTypeInfo] a__Type ON a__Type.[Guid] = a.[TypeGuid] RIGHT JOIN [TestTypeParentInfo] c ON c.[Id] = a__Type.[ParentId]", sql);
            query.ToList();

            //���û�е�������b��c������ϵ
            var query2 = select.From<TestTypeInfo, TestTypeParentInfo>((s, b, c) => s
                 .RightJoin(a => a.TypeGuid == b.Guid)
                 .RightJoin(a => b.ParentId == c.Id));
            sql = query2.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], b.[Guid], b.[ParentId], b.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22] a RIGHT JOIN [TestTypeInfo] b ON a.[TypeGuid] = b.[Guid] RIGHT JOIN [TestTypeParentInfo] c ON b.[ParentId] = c.[Id]", sql);
            query2.ToList();

            //������϶����㲻��
            query = select.RightJoin("TestTypeInfo b on b.Guid = a.TypeGuid");
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime] FROM [tb_topic22] a RIGHT JOIN TestTypeInfo b on b.Guid = a.TypeGuid", sql);
            query.ToList();

            query = select.RightJoin("TestTypeInfo b on b.Guid = a.TypeGuid and b.Name = @bname", new { bname = "xxx" });
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime] FROM [tb_topic22] a RIGHT JOIN TestTypeInfo b on b.Guid = a.TypeGuid and b.Name = @bname", sql);
            query.ToList();

        }
        [Fact]
        public void Where()
        {
            var sqltmp1 = select.Where(a => a.Id == 0 && (a.Title == "x" || a.Title == "y") && a.Clicks == 1).ToSql();
            var sqltmp2 = select.Where(a => a.Id.Equals(true) && (a.Title.Equals("x") || a.Title.Equals("y")) && a.Clicks.Equals(1)).ToSql();
            var sqltmp3 = select.Where(a => a.Id == 0).Where(a => ((a.Title == "x" && a.Title == "z") || a.Title == "y")).ToSql();

            var sqltmp4 = select.Where(a => (a.Id - 10) / 2 > 0).ToSql();

            //����е�������a.Type��a.Type.Parent ���ǵ�������
            var query = select.Where(a => a.Id == 10);
            var sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime] FROM [tb_topic22] a WHERE (a.[Id] = 10)", sql);
            query.ToList();

            query = select.Where(a => a.Id == 10 && a.Id > 10 || a.Clicks > 100);
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime] FROM [tb_topic22] a WHERE ((a.[Id] = 10 AND a.[Id] > 10 OR a.[Clicks] > 100))", sql);
            query.ToList();

            query = select.Where(a => a.Id == 10).Where(a => a.Clicks > 100);
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime] FROM [tb_topic22] a WHERE (a.[Id] = 10) AND (a.[Clicks] > 100)", sql);
            query.ToList();

            query = select.Where(a => a.Type.Name == "typeTitle");
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a__Type.[Guid], a__Type.[ParentId], a__Type.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22] a LEFT JOIN [TestTypeInfo] a__Type ON a__Type.[Guid] = a.[TypeGuid] WHERE (a__Type.[Name] = N'typeTitle')", sql);
            query.ToList();

            query = select.Where(a => a.Type.Name == "typeTitle" && a.Type.Guid == a.TypeGuid);
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a__Type.[Guid], a__Type.[ParentId], a__Type.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22] a LEFT JOIN [TestTypeInfo] a__Type ON a__Type.[Guid] = a.[TypeGuid] WHERE (a__Type.[Name] = N'typeTitle' AND a__Type.[Guid] = a.[TypeGuid])", sql);
            query.ToList();

            query = select.Where(a => a.Type.Parent.Name == "tparent");
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a__Type.[Guid], a__Type.[ParentId], a__Type.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22] a LEFT JOIN [TestTypeInfo] a__Type ON a__Type.[Guid] = a.[TypeGuid] LEFT JOIN [TestTypeParentInfo] a__Type__Parent ON a__Type__Parent.[Id] = a__Type.[ParentId] WHERE (a__Type__Parent.[Name] = N'tparent')", sql);
            query.ToList();

            //���û�е������ԣ��򵥶������
            query = select.Where<TestTypeInfo>((a, b) => b.Guid == a.TypeGuid && b.Name == "typeTitle");
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime] FROM [tb_topic22] a, [TestTypeInfo] b WHERE (b.[Guid] = a.[TypeGuid] AND b.[Name] = N'typeTitle')", sql);
            query.ToList();

            query = select.Where<TestTypeInfo>((a, b) => b.Name == "typeTitle" && b.Guid == a.TypeGuid);
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime] FROM [tb_topic22] a, [TestTypeInfo] b WHERE (b.[Name] = N'typeTitle' AND b.[Guid] = a.[TypeGuid])", sql);
            query.ToList();

            query = select.Where<TestTypeInfo, TestTypeParentInfo>((a, b, c) => c.Name == "tparent");
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime] FROM [tb_topic22] a, [TestTypeParentInfo] c WHERE (c.[Name] = N'tparent')", sql);
            query.ToList();

            //����һ�� From ��Ķ������
            var query2 = select.From<TestTypeInfo, TestTypeParentInfo>((s, b, c) => s
                .Where(a => a.Id == 10 && c.Name == "xxx")
                .Where(a => b.ParentId == 20));
            sql = query2.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime] FROM [tb_topic22] a, [TestTypeInfo] b, [TestTypeParentInfo] c WHERE (a.[Id] = 10 AND c.[Name] = N'xxx') AND (b.[ParentId] = 20)", sql);
            query2.ToList();

            //������϶����㲻��
            query = select.Where("a.Clicks > 100 and a.Id = @id", new { id = 10 });
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime] FROM [tb_topic22] a WHERE (a.Clicks > 100 and a.Id = @id)", sql);
            query.ToList();
        }
        [Fact]
        public void WhereIf()
        {
            //����е�������a.Type��a.Type.Parent ���ǵ�������
            var query = select.WhereIf(true, a => a.Id == 10);
            var sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime] FROM [tb_topic22] a WHERE (a.[Id] = 10)", sql);
            query.ToList();

            query = select.WhereIf(true, a => a.Id == 10 && a.Id > 10 || a.Clicks > 100);
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime] FROM [tb_topic22] a WHERE ((a.[Id] = 10 AND a.[Id] > 10 OR a.[Clicks] > 100))", sql);
            query.ToList();

            query = select.WhereIf(true, a => a.Id == 10).WhereIf(true, a => a.Clicks > 100);
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime] FROM [tb_topic22] a WHERE (a.[Id] = 10) AND (a.[Clicks] > 100)", sql);
            query.ToList();

            query = select.WhereIf(true, a => a.Type.Name == "typeTitle");
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a__Type.[Guid], a__Type.[ParentId], a__Type.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22] a LEFT JOIN [TestTypeInfo] a__Type ON a__Type.[Guid] = a.[TypeGuid] WHERE (a__Type.[Name] = N'typeTitle')", sql);
            query.ToList();

            query = select.WhereIf(true, a => a.Type.Name == "typeTitle" && a.Type.Guid == a.TypeGuid);
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a__Type.[Guid], a__Type.[ParentId], a__Type.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22] a LEFT JOIN [TestTypeInfo] a__Type ON a__Type.[Guid] = a.[TypeGuid] WHERE (a__Type.[Name] = N'typeTitle' AND a__Type.[Guid] = a.[TypeGuid])", sql);
            query.ToList();

            query = select.WhereIf(true, a => a.Type.Parent.Name == "tparent");
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a__Type.[Guid], a__Type.[ParentId], a__Type.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22] a LEFT JOIN [TestTypeInfo] a__Type ON a__Type.[Guid] = a.[TypeGuid] LEFT JOIN [TestTypeParentInfo] a__Type__Parent ON a__Type__Parent.[Id] = a__Type.[ParentId] WHERE (a__Type__Parent.[Name] = N'tparent')", sql);
            query.ToList();

            //����һ�� From ��Ķ������
            var query2 = select.From<TestTypeInfo, TestTypeParentInfo>((s, b, c) => s
                .WhereIf(true, a => a.Id == 10 && c.Name == "xxx")
                .WhereIf(true, a => b.ParentId == 20));
            sql = query2.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime] FROM [tb_topic22] a, [TestTypeInfo] b, [TestTypeParentInfo] c WHERE (a.[Id] = 10 AND c.[Name] = N'xxx') AND (b.[ParentId] = 20)", sql);
            query2.ToList();

            //������϶����㲻��
            query = select.WhereIf(true, "a.Clicks > 100 and a.Id = @id", new { id = 10 });
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime] FROM [tb_topic22] a WHERE (a.Clicks > 100 and a.Id = @id)", sql);
            query.ToList();

            // ==========================================WhereIf(false)

            //����е�������a.Type��a.Type.Parent ���ǵ�������
            query = select.WhereIf(false, a => a.Id == 10);
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime] FROM [tb_topic22] a", sql);
            query.ToList();

            query = select.WhereIf(false, a => a.Id == 10 && a.Id > 10 || a.Clicks > 100);
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime] FROM [tb_topic22] a", sql);
            query.ToList();

            query = select.WhereIf(false, a => a.Id == 10).WhereIf(false, a => a.Clicks > 100);
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime] FROM [tb_topic22] a", sql);
            query.ToList();

            query = select.WhereIf(false, a => a.Type.Name == "typeTitle");
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime] FROM [tb_topic22] a", sql);
            query.ToList();

            query = select.WhereIf(false, a => a.Type.Name == "typeTitle" && a.Type.Guid == a.TypeGuid);
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime] FROM [tb_topic22] a", sql);
            query.ToList();

            query = select.WhereIf(false, a => a.Type.Parent.Name == "tparent");
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime] FROM [tb_topic22] a", sql);
            query.ToList();

            //����һ�� From ��Ķ������
            query2 = select.From<TestTypeInfo, TestTypeParentInfo>((s, b, c) => s
                .WhereIf(false, a => a.Id == 10 && c.Name == "xxx")
                .WhereIf(false, a => b.ParentId == 20));
            sql = query2.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime] FROM [tb_topic22] a, [TestTypeInfo] b, [TestTypeParentInfo] c", sql);
            query2.ToList();

            //������϶����㲻��
            query = select.WhereIf(false, "a.Clicks > 100 and a.Id = @id", new { id = 10 });
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime] FROM [tb_topic22] a", sql);
            query.ToList();
        }
        [Fact]
        public void WhereExists()
        {
            var sql2222 = select.Where(a => select.Where(b => b.Id == a.Id).Any()).ToList();

            sql2222 = select.Where(a =>
                select.Where(b => b.Id == a.Id && select.Where(c => c.Id == b.Id).Where(d => d.Id == a.Id).Where(e => e.Id == b.Id)

                //.Offset(a.Id)

                .Any()
                ).Any()
            ).ToList();

            var sqlAliasA = select.Where(p => select.Where(a => a.Id == p.Id).Any()).ToSql();
            Assert.Equal(@"SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime] 
FROM [tb_topic22] a 
WHERE (exists(SELECT TOP 1 1 
    FROM [tb_topic22] sub_a 
    WHERE (sub_a.[Id] = a.[Id])))", sqlAliasA);
        }
        [Fact]
        public void GroupBy()
        {
            var groupby = select.From<TestTypeInfo, TestTypeParentInfo>((s, b, c) => s
                .Where(a => a.Id == 1)
            )
            .GroupBy((a, b, c) => new { tt2 = a.Title.Substring(0, 2), mod4 = a.Id % 4 })
            .Having(a => a.Count() > 0 && a.Avg(a.Key.mod4) > 0 && a.Max(a.Key.mod4) > 0)
            .Having(a => a.Count() < 300 || a.Avg(a.Key.mod4) < 100)
            .OrderBy(a => a.Key.tt2)
            .OrderByDescending(a => a.Count())
            .Offset(10)
            .Limit(2)
            .Count(out var trycount)
            .ToList(a => new
            {
                a.Key.tt2,
                cou1 = a.Count(),
                cou2 = a.Count(a.Value.Item3.Id),
                arg1 = a.Avg(a.Key.mod4),
                ccc2 = a.Key.tt2 ?? "now()",
                //ccc = Convert.ToDateTime("now()"), partby = Convert.ToDecimal("sum(num) over(PARTITION BY server_id,os,rid,chn order by id desc)")
                ccc3 = a.Max(a.Value.Item3.Id)
            });

            var testpid1 = g.sqlserver.Insert<TestTypeInfo>().AppendData(new TestTypeInfo { Name = "Name" + DateTime.Now.ToString("yyyyMMddHHmmss") }).ExecuteIdentity();
            g.sqlserver.Insert<TestInfo>().AppendData(new TestInfo { Title = "Title" + DateTime.Now.ToString("yyyyMMddHHmmss"), CreateTime = DateTime.Now, TypeGuid = (int)testpid1 }).ExecuteAffrows();

            var fkfjfj = select.GroupBy(a => a.Title)
                .ToList(a => a.Sum(a.Value.TypeGuid));

            var aggsql1 = select
                .GroupBy(a => a.Title)
                .ToSql(b => new
                {
                    b.Key,
                    cou = b.Count(),
                    sum2 = b.Sum(b.Value.TypeGuid)
                });
            var aggtolist1 = select
                .GroupBy(a => a.Title)
                .ToList(b => new
                {
                    b.Key,
                    cou = b.Count(),
                    sum2 = b.Sum(b.Value.TypeGuid)
                });
            var aggtolist11 = select
                .GroupBy(a => a.Title)
                .ToDictionary(b => new
                {
                    b.Key,
                    cou = b.Count(),
                    sum2 = b.Sum(b.Value.TypeGuid)
                });

            var aggsql2 = select
                .GroupBy(a => new { a.Title, yyyy = string.Concat(a.CreateTime.Year, '-', a.CreateTime.Month) })
                .ToSql(b => new
                {
                    b.Key.Title,
                    b.Key.yyyy,

                    cou = b.Count(),
                    sum2 = b.Sum(b.Value.TypeGuid)
                });
            var aggtolist2 = select
                .GroupBy(a => new { a.Title, yyyy = string.Concat(a.CreateTime.Year, '-', a.CreateTime.Month) })
                .ToList(b => new
                {
                    b.Key.Title,
                    b.Key.yyyy,

                    cou = b.Count(),
                    sum2 = b.Sum(b.Value.TypeGuid)
                });
            var aggtolist22 = select
                .GroupBy(a => new { a.Title, yyyy = string.Concat(a.CreateTime.Year, '-', a.CreateTime.Month) })
                .ToDictionary(b => new
                {
                    b.Key.Title,
                    b.Key.yyyy,
                    b.Key,
                    cou = b.Count(),
                    sum2 = b.Sum(b.Value.TypeGuid)
                });

            var aggsql3 = select
                .GroupBy(a => a.Title)
                .ToSql(b => new
                {
                    b.Key,
                    cou = b.Count(),
                    sum2 = b.Sum(b.Value.TypeGuid),
                    sum3 = b.Sum(b.Value.Type.Parent.Id)
                });
        }
        [Fact]
        public void ToAggregate()
        {
            var sql = select.ToAggregate(a => new { sum = a.Sum(a.Key.Id + 11.11), avg = a.Avg(Convert.ToInt64(a.Key.Id)), count = a.Count(), max = a.Max(a.Key.Id), min = a.Min(a.Key.Id) });
        }
        [Fact]
        public void OrderBy()
        {
            var sql = select.OrderBy(a => new Random().NextDouble()).ToList();
        }
        [Fact]
        public void OrderByRandom()
        {
            var t1 = select.OrderByRandom().Limit(10).ToSql("1");
            Assert.Equal(@"SELECT TOP 10 1 
FROM [tb_topic22] a 
ORDER BY newid()", t1);
            var t2 = select.OrderByRandom().Limit(10).ToList();
        }

        [Fact]
        public void Skip_Offset()
        {
            var list = select.Offset(10).Limit(10).ToList();

            var sqlSkip = select.Skip(10).ToSql();
            Assert.Equal(@"SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime] 
FROM [tb_topic22] a 
ORDER BY a.[Id] 
OFFSET 10 ROW", sqlSkip);
            list = select.Skip(10).ToList();

            using (var fsql = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.SqlServer, "Data Source=.;Integrated Security=True;Initial Catalog=freesqlTest;Pooling=true;Max Pool Size=3;TrustServerCertificate=true")
                .UseAutoSyncStructure(true)
                .UseMonitorCommand(
                    cmd => Trace.WriteLine("\r\n线程" + Thread.CurrentThread.ManagedThreadId + ": " + cmd.CommandText) //监听SQL命令对象，在执行前
                    //, (cmd, traceLog) => Console.WriteLine(traceLog)
                    )
                .Build())
            {
                var commUtils = (fsql.Select<object>() as Select0Provider)._commonUtils as SqlServerUtils;
                commUtils.ServerVersion = 9;
                sqlSkip = fsql.Select<Topic>().Skip(10).ToSql();
                Assert.Equal(@"WITH t AS ( SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime], ROW_NUMBER() OVER(ORDER BY a.[Id]) AS __rownum__ 
FROM [tb_topic22] a ) SELECT t.* FROM t where __rownum__ > 10", sqlSkip);
                list = fsql.Select<Topic>().Skip(10).ToList();

                sqlSkip = fsql.Select<Topic>().Limit(10).ToSql();
                Assert.Equal(@"SELECT TOP 10 a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime] 
FROM [tb_topic22] a 
ORDER BY a.[Id]", sqlSkip);
                list = fsql.Select<Topic>().Limit(10).ToList();

                sqlSkip = fsql.Select<Topic>().Skip(10).Limit(10).ToSql();
                Assert.Equal(@"WITH t AS ( SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime], ROW_NUMBER() OVER(ORDER BY a.[Id]) AS __rownum__ 
FROM [tb_topic22] a ) SELECT t.* FROM t where __rownum__ between 11 and 20", sqlSkip);
                list = fsql.Select<Topic>().Skip(10).Limit(10).ToList();


                sqlSkip = fsql.Select<Topic>().Skip(10).OrderBy(a => a.Title).ToSql();
                Assert.Equal(@"WITH t AS ( SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime], ROW_NUMBER() OVER(ORDER BY a.[Title]) AS __rownum__ 
FROM [tb_topic22] a ) SELECT t.* FROM t where __rownum__ > 10", sqlSkip);
                list = fsql.Select<Topic>().Skip(10).OrderBy(a => a.Title).ToList();

                sqlSkip = fsql.Select<Topic>().Limit(10).OrderBy(a => a.Title).ToSql();
                Assert.Equal(@"SELECT TOP 10 a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime] 
FROM [tb_topic22] a 
ORDER BY a.[Title]", sqlSkip);
                list = fsql.Select<Topic>().Limit(10).OrderBy(a => a.Title).ToList();

                sqlSkip = fsql.Select<Topic>().Skip(10).Limit(10).OrderBy(a => a.Title).ToSql();
                Assert.Equal(@"WITH t AS ( SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime], ROW_NUMBER() OVER(ORDER BY a.[Title]) AS __rownum__ 
FROM [tb_topic22] a ) SELECT t.* FROM t where __rownum__ between 11 and 20", sqlSkip);
                list = fsql.Select<Topic>().Skip(10).Limit(10).OrderBy(a => a.Title).ToList();
            }
        }

        [Fact]
        public void Take_Limit()
        {
            var sql = select.Limit(10).ToList();
        }
        [Fact]
        public void Page()
        {
            var sql1 = select.Page(1, 10).ToList();
            var sql2 = select.Page(2, 10).ToList();
            var sql3 = select.Page(3, 10).ToList();

            var sql11 = select.OrderBy(a => new Random().NextDouble()).Page(1, 10).ToList();
            var sql22 = select.OrderBy(a => new Random().NextDouble()).Page(2, 10).ToList();
            var sql33 = select.OrderBy(a => new Random().NextDouble()).Page(3, 10).ToList();
        }
        [Fact]
        public void Distinct()
        {
            var t1 = select.Distinct().ToList(a => a.Title);
            var t2 = select.Distinct().Limit(10).ToList(a => a.Title);
        }

        [Fact]
        public void Sum()
        {
            var subquery = select.ToSql(a => new
            {
                all = a,
                count = (long)select.As("b").Sum(b => b.Id)
            });
            Assert.Equal(@"SELECT a.[Id] as1, a.[Clicks] as2, a.[TypeGuid] as3, a.[Title] as4, a.[CreateTime] as5, isnull((SELECT sum(b.[Id]) 
    FROM [tb_topic22] b), 0) as6 
FROM [tb_topic22] a", subquery);
            var subqueryList = select.ToList(a => new
            {
                all = a,
                count = (long)select.As("b").Sum(b => b.Id)
            });
        }
        [Fact]
        public void Min()
        {
            var subquery = select.ToSql(a => new
            {
                all = a,
                min = select.As("b").Min(b => b.Id),
                min2 = select.As("b").Min(b => b.CreateTime)
            });
            Assert.Equal(@"SELECT a.[Id] as1, a.[Clicks] as2, a.[TypeGuid] as3, a.[Title] as4, a.[CreateTime] as5, isnull((SELECT min(b.[Id]) 
    FROM [tb_topic22] b), 0) as6, isnull((SELECT min(b.[CreateTime]) 
    FROM [tb_topic22] b), '1970-01-01 00:00:00.000') as7 
FROM [tb_topic22] a", subquery);
            var subqueryList = select.ToList(a => new
            {
                all = a,
                min = select.As("b").Min(b => b.Id),
                min2 = select.As("b").Min(b => b.CreateTime),
                min3 = select.As("b").Where(b => b.Id < 0).Min(b => b.CreateTime)
            });
        }
        [Fact]
        public void Max()
        {
            var subquery = select.ToSql(a => new
            {
                all = a,
                max = select.As("b").Max(b => b.Id),
                max2 = select.As("b").Max(b => b.CreateTime)
            });
            Assert.Equal(@"SELECT a.[Id] as1, a.[Clicks] as2, a.[TypeGuid] as3, a.[Title] as4, a.[CreateTime] as5, isnull((SELECT max(b.[Id]) 
    FROM [tb_topic22] b), 0) as6, isnull((SELECT max(b.[CreateTime]) 
    FROM [tb_topic22] b), '1970-01-01 00:00:00.000') as7 
FROM [tb_topic22] a", subquery);
            var subqueryList = select.ToList(a => new
            {
                all = a,
                max = select.As("b").Max(b => b.Id),
                max2 = select.As("b").Max(b => b.CreateTime)
            });
        }
        [Fact]
        public void Avg()
        {
            var subquery = select.ToSql(a => new
            {
                all = a,
                count = select.As("b").Avg(b => b.Id)
            });
            Assert.Equal(@"SELECT a.[Id] as1, a.[Clicks] as2, a.[TypeGuid] as3, a.[Title] as4, a.[CreateTime] as5, isnull((SELECT avg(b.[Id]) 
    FROM [tb_topic22] b), 0) as6 
FROM [tb_topic22] a", subquery);
            var subqueryList = select.ToList(a => new
            {
                all = a,
                count = select.As("b").Avg(b => b.Id)
            });
        }
        [Fact]
        public void WhereIn()
        {
            var subquery = select.Where(a => select.As("b").ToList(b => b.Title).Contains(a.Id.ToString())).ToSql();
            Assert.Equal(@"SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime] 
FROM [tb_topic22] a 
WHERE (((cast(a.[Id] as nvarchar(100))) in (SELECT b.[Title] 
    FROM [tb_topic22] b)))", subquery);
            var subqueryList = select.Where(a => select.As("b").ToList(b => b.Title).Contains(a.Id.ToString())).ToList();

            subquery = select.Where(a => select.As("b").Limit(10).ToList(b => b.Title).Contains(a.Id.ToString())).ToSql();
            Assert.Equal(@"SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime] 
FROM [tb_topic22] a 
WHERE (((cast(a.[Id] as nvarchar(100))) in (SELECT TOP 10 b.[Title] 
    FROM [tb_topic22] b)))", subquery);
            subqueryList = select.Where(a => select.As("b").Limit(10).ToList(b => b.Title).Contains(a.Id.ToString())).ToList();
        }
        [Fact]
        public void As()
        {
        }

        [Fact]
        public void AsTable()
        {

            var listt = select.AsTable((a, b) => "(select * from tb_topic22 where Clicks > 10)").Page(1, 10).ToList();

            Func<Type, string, string> tableRule = (type, oldname) =>
            {
                if (type == typeof(Topic)) return oldname + "AsTable1";
                else if (type == typeof(TestTypeInfo)) return oldname + "AsTable2";
                return oldname + "AsTable";
            };

            //����е�������a.Type��a.Type.Parent ���ǵ�������
            var query = select.LeftJoin(a => a.Type.Guid == a.TypeGuid).AsTable(tableRule);
            var sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a__Type.[Guid], a__Type.[ParentId], a__Type.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22AsTable1] a LEFT JOIN [TestTypeInfoAsTable2] a__Type ON a__Type.[Guid] = a.[TypeGuid]", sql);

            query = select.LeftJoin(a => a.Type.Guid == a.TypeGuid && a.Type.Name == "xxx").AsTable(tableRule);
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a__Type.[Guid], a__Type.[ParentId], a__Type.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22AsTable1] a LEFT JOIN [TestTypeInfoAsTable2] a__Type ON a__Type.[Guid] = a.[TypeGuid] AND a__Type.[Name] = N'xxx'", sql);

            query = select.LeftJoin(a => a.Type.Guid == a.TypeGuid && a.Type.Name == "xxx").Where(a => a.Type.Parent.Id == 10).AsTable(tableRule);
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a__Type.[Guid], a__Type.[ParentId], a__Type.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22AsTable1] a LEFT JOIN [TestTypeInfoAsTable2] a__Type ON a__Type.[Guid] = a.[TypeGuid] AND a__Type.[Name] = N'xxx' LEFT JOIN [TestTypeParentInfoAsTable] a__Type__Parent ON a__Type__Parent.[Id] = a__Type.[ParentId] WHERE (a__Type__Parent.[Id] = 10)", sql);

            //���û�е�������
            query = select.LeftJoin<TestTypeInfo>((a, b) => b.Guid == a.TypeGuid).AsTable(tableRule);
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], b.[Guid], b.[ParentId], b.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22AsTable1] a LEFT JOIN [TestTypeInfoAsTable2] b ON b.[Guid] = a.[TypeGuid]", sql);

            query = select.LeftJoin<TestTypeInfo>((a, b) => b.Guid == a.TypeGuid && b.Name == "xxx").AsTable(tableRule);
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], b.[Guid], b.[ParentId], b.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22AsTable1] a LEFT JOIN [TestTypeInfoAsTable2] b ON b.[Guid] = a.[TypeGuid] AND b.[Name] = N'xxx'", sql);

            query = select.LeftJoin<TestTypeInfo>((a, b) => b.Guid == a.TypeGuid && b.Name == "xxx").Where(a => a.Type.Parent.Id == 10).AsTable(tableRule);
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a__Type.[Guid], a__Type.[ParentId], a__Type.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22AsTable1] a LEFT JOIN [TestTypeInfoAsTable2] b ON b.[Guid] = a.[TypeGuid] AND b.[Name] = N'xxx' LEFT JOIN [TestTypeInfoAsTable2] a__Type ON a__Type.[Guid] = a.[TypeGuid] LEFT JOIN [TestTypeParentInfoAsTable] a__Type__Parent ON a__Type__Parent.[Id] = a__Type.[ParentId] WHERE (a__Type__Parent.[Id] = 10)", sql);

            //�������
            query = select
                .LeftJoin(a => a.Type.Guid == a.TypeGuid)
                .LeftJoin(a => a.Type.Parent.Id == a.Type.ParentId).AsTable(tableRule);
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a__Type.[Guid], a__Type.[ParentId], a__Type.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22AsTable1] a LEFT JOIN [TestTypeInfoAsTable2] a__Type ON a__Type.[Guid] = a.[TypeGuid] LEFT JOIN [TestTypeParentInfoAsTable] a__Type__Parent ON a__Type__Parent.[Id] = a__Type.[ParentId]", sql);

            query = select
                .LeftJoin<TestTypeInfo>((a, b) => b.Guid == a.TypeGuid)
                .LeftJoin<TestTypeParentInfo>((a, c) => c.Id == a.Type.ParentId).AsTable(tableRule);
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], b.[Guid], b.[ParentId], b.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22AsTable1] a LEFT JOIN [TestTypeInfoAsTable2] a__Type ON a__Type.[Guid] = a.[TypeGuid] LEFT JOIN [TestTypeInfoAsTable2] b ON b.[Guid] = a.[TypeGuid] LEFT JOIN [TestTypeParentInfoAsTable] c ON c.[Id] = a__Type.[ParentId]", sql);

            //���û�е�������b��c������ϵ
            var query2 = select.From<TestTypeInfo, TestTypeParentInfo>((s, b, c) => s
                 .LeftJoin(a => a.TypeGuid == b.Guid)
                 .LeftJoin(a => b.ParentId == c.Id)).AsTable(tableRule);
            sql = query2.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], b.[Guid], b.[ParentId], b.[Name], a.[Title], a.[CreateTime] FROM [tb_topic22AsTable1] a LEFT JOIN [TestTypeInfoAsTable2] b ON a.[TypeGuid] = b.[Guid] LEFT JOIN [TestTypeParentInfoAsTable] c ON b.[ParentId] = c.[Id]", sql);

            //������϶����㲻��
            query = select.LeftJoin("TestTypeInfo b on b.Guid = a.TypeGuid").AsTable(tableRule);
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime] FROM [tb_topic22AsTable1] a LEFT JOIN TestTypeInfo b on b.Guid = a.TypeGuid", sql);

            query = select.LeftJoin("TestTypeInfo b on b.Guid = a.TypeGuid and b.Name = @bname", new { bname = "xxx" }).AsTable(tableRule);
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime] FROM [tb_topic22AsTable1] a LEFT JOIN TestTypeInfo b on b.Guid = a.TypeGuid and b.Name = @bname", sql);

            query = select.AsTable((_, old) => old).AsTable((_, old) => old);
            sql = query.ToSql().Replace("\r\n", "");
            Assert.Equal("SELECT  * from (SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime] FROM [tb_topic22] a) ftb UNION ALL SELECT  * from (SELECT a.[Id], a.[Clicks], a.[TypeGuid], a.[Title], a.[CreateTime] FROM [tb_topic22] a) ftb", sql);
            query.ToList();

            query = select.AsTable((_, old) => old).AsTable((_, old) => old);
            sql = query.ToSql("count(1) as1").Replace("\r\n", "");
            Assert.Equal("SELECT  * from (SELECT count(1) as1 FROM [tb_topic22] a) ftb UNION ALL SELECT  * from (SELECT count(1) as1 FROM [tb_topic22] a) ftb", sql);
            query.Count();

            select.AsTable((_, old) => old).AsTable((_, old) => old).Max(a => a.Id);
            select.AsTable((_, old) => old).AsTable((_, old) => old).Min(a => a.Id);
            select.AsTable((_, old) => old).AsTable((_, old) => old).Sum(a => a.Id);
            select.AsTable((_, old) => old).AsTable((_, old) => old).Avg(a => a.Id);

            var sqlsss = select
                .AsTable((type, old) => type == typeof(Topic) ? $"{old}_1" : null)
                .AsTable((type, old) => type == typeof(Topic) ? $"{old}_2" : null)
                .ToSql(a => new
                {
                    a.Id,
                    a.Clicks
                }, FieldAliasOptions.AsProperty);

            var slsld3 = select
                .AsTable((type, old) => type == typeof(Topic) ? $"({sqlsss})" : null)
                .Page(1, 20)
                .ToList(a => new
                {
                    a.Id,
                    a.Clicks
                });

            var testUnionAll = select
                .WithSql("SELECT * FROM [tb_topic22] where Id = @id1", new { id1 = 10 })
                .WithSql("SELECT * FROM [tb_topic22] where Id = @id2", new { id2 = 11 })
                .ToSql(a => new
                {
                    a.Id,
                    a.Clicks
                });

            var testUnionAllToList = select
                .WithSql("SELECT * FROM [tb_topic22] where Id = @id1", new { id1 = 10 })
                .WithSql("SELECT * FROM [tb_topic22] where Id = @id2", new { id2 = 11 })
                .ToList(a => new
                {
                    a.Id,
                    a.Clicks
                });

            var testUnionAll2 = g.sqlite.Select<object>()
                .WithSql("SELECT * FROM [tb_topic22] where Id = 10")
                .WithSql("SELECT * FROM [tb_topic22] where Id = 11")
                .ToDataTable("*");

            var multiWithSql = g.sqlite.Select<TestInclude_OneToManyModel1, TestInclude_OneToManyModel2, TestInclude_OneToManyModel3>()
                .WithSql(
                    "select * from TestInclude_OneToManyModel1 where Id=@id1",
                    "select * from TestInclude_OneToManyModel2 where model2id=@id2",
                    null,
                    new { id1 = 10, id2 = 11, id3 = 13 }
                )
                .LeftJoin((a, b, c) => b.model2id == a.id)
                .LeftJoin((a, b, c) => c.model2111Idaaa == b.model2id)
                .ToList();
        }

        public class TestInclude_OneToManyModel1
        {
            [Column(IsIdentity = true)]
            public int id { get; set; }
            public virtual TestInclude_OneToManyModel2 model2 { get; set; }

            public string m1name { get; set; }
        }
        public class TestInclude_OneToManyModel2
        {
            [Column(IsPrimary = true)]
            public int model2id { get; set; }
            public virtual TestInclude_OneToManyModel1 model1 { get; set; }

            public string m2setting { get; set; }

            public List<TestInclude_OneToManyModel3> childs { get; set; }
        }
        public class TestInclude_OneToManyModel3
        {
            [Column(IsIdentity = true)]
            public int id { get; set; }

            public int model2111Idaaa { get; set; }
            public string title { get; set; }

            public List<TestInclude_OneToManyModel4> childs2 { get; set; }
        }
        public class TestInclude_OneToManyModel4
        {
            [Column(IsIdentity = true)]
            public int id { get; set; }

            public int model3333Id333 { get; set; }
            public string title444 { get; set; }
        }

        [Fact]
        public void Include_OneToMany()
        {
            var model1 = new TestInclude_OneToManyModel1 { m1name = DateTime.Now.Second.ToString() };
            model1.id = (int)g.sqlserver.Insert(model1).ExecuteIdentity();
            var model2 = new TestInclude_OneToManyModel2 { model2id = model1.id, m2setting = DateTime.Now.Second.ToString() };
            g.sqlserver.Insert(model2).ExecuteAffrows();

            var model3_1 = new TestInclude_OneToManyModel3 { model2111Idaaa = model1.id, title = "testmodel3__111" };
            model3_1.id = (int)g.sqlserver.Insert(model3_1).ExecuteIdentity();
            var model3_2 = new TestInclude_OneToManyModel3 { model2111Idaaa = model1.id, title = "testmodel3__222" };
            model3_2.id = (int)g.sqlserver.Insert(model3_2).ExecuteIdentity();
            var model3_3 = new TestInclude_OneToManyModel3 { model2111Idaaa = model1.id, title = "testmodel3__333" };
            model3_3.id = (int)g.sqlserver.Insert(model3_2).ExecuteIdentity();

            var model4s = new[] {
                new TestInclude_OneToManyModel4{ model3333Id333 = model3_1.id, title444 = "testmodel3_4__111" },
                new TestInclude_OneToManyModel4{ model3333Id333 = model3_1.id, title444 = "testmodel3_4__222" },
                new TestInclude_OneToManyModel4{ model3333Id333 = model3_2.id, title444 = "testmodel3_4__111" },
                new TestInclude_OneToManyModel4{ model3333Id333 = model3_2.id, title444 = "testmodel3_4__222" },
                new TestInclude_OneToManyModel4{ model3333Id333 = model3_2.id, title444 = "testmodel3_4__333" }
            };
            Assert.Equal(5, g.sqlserver.Insert(model4s).ExecuteAffrows());

            var t0 = g.sqlserver.Select<TestInclude_OneToManyModel2>()
                .IncludeMany(a => a.childs.Where(m3 => m3.model2111Idaaa == a.model2id))
                .Where(a => a.model2id <= model1.id)
                .ToList();

            var t1 = g.sqlserver.Select<TestInclude_OneToManyModel1>()
                .IncludeMany(a => a.model2.childs.Where(m3 => m3.model2111Idaaa == a.model2.model2id))
                .Where(a => a.id <= model1.id)
                .ToList();

            var t2 = g.sqlserver.Select<TestInclude_OneToManyModel1>()
                .IncludeMany(a => a.model2.childs.Where(m3 => m3.model2111Idaaa == a.model2.model2id),
                    then => then.IncludeMany(m3 => m3.childs2.Where(m4 => m4.model3333Id333 == m3.id)))
                .Where(a => a.id <= model1.id)
                .ToList();

            var t00 = g.sqlserver.Select<TestInclude_OneToManyModel2>()
                .IncludeMany(a => a.childs.Take(1).Where(m3 => m3.model2111Idaaa == a.model2id))
                .Where(a => a.model2id <= model1.id)
                .ToList();

            var t11 = g.sqlserver.Select<TestInclude_OneToManyModel1>()
                .IncludeMany(a => a.model2.childs.Take(1).Where(m3 => m3.model2111Idaaa == a.model2.model2id))
                .Where(a => a.id <= model1.id)
                .ToList();

            var t22 = g.sqlserver.Select<TestInclude_OneToManyModel1>()
                .IncludeMany(a => a.model2.childs.Take(1).Where(m3 => m3.model2111Idaaa == a.model2.model2id),
                    then => then.IncludeMany(m3 => m3.childs2.Take(2).Where(m4 => m4.model3333Id333 == m3.id)))
                .Where(a => a.id <= model1.id)
                .ToList();

            //---- Select ----

            var at0 = g.sqlserver.Select<TestInclude_OneToManyModel2>()
                .IncludeMany(a => a.childs.Where(m3 => m3.model2111Idaaa == a.model2id).Select(m3 => new TestInclude_OneToManyModel3 { id = m3.id }))
                .Where(a => a.model2id <= model1.id)
                .ToList();

            var at1 = g.sqlserver.Select<TestInclude_OneToManyModel1>()
                .IncludeMany(a => a.model2.childs.Where(m3 => m3.model2111Idaaa == a.model2.model2id).Select(m3 => new TestInclude_OneToManyModel3 { id = m3.id }))
                .Where(a => a.id <= model1.id)
                .ToList();

            var at2 = g.sqlserver.Select<TestInclude_OneToManyModel1>()
                .IncludeMany(a => a.model2.childs.Where(m3 => m3.model2111Idaaa == a.model2.model2id).Select(m3 => new TestInclude_OneToManyModel3 { id = m3.id }),
                    then => then.IncludeMany(m3 => m3.childs2.Where(m4 => m4.model3333Id333 == m3.id).Select(m4 => new TestInclude_OneToManyModel4 { id = m4.id })))
                .Where(a => a.id <= model1.id)
                .ToList();

            var at00 = g.sqlserver.Select<TestInclude_OneToManyModel2>()
                .IncludeMany(a => a.childs.Take(1).Where(m3 => m3.model2111Idaaa == a.model2id).Select(m3 => new TestInclude_OneToManyModel3 { id = m3.id }))
                .Where(a => a.model2id <= model1.id)
                .ToList();

            var at11 = g.sqlserver.Select<TestInclude_OneToManyModel1>()
                .IncludeMany(a => a.model2.childs.Take(1).Where(m3 => m3.model2111Idaaa == a.model2.model2id).Select(m3 => new TestInclude_OneToManyModel3 { id = m3.id }))
                .Where(a => a.id <= model1.id)
                .ToList();

            var at22 = g.sqlserver.Select<TestInclude_OneToManyModel1>()
                .IncludeMany(a => a.model2.childs.Take(1).Where(m3 => m3.model2111Idaaa == a.model2.model2id).Select(m3 => new TestInclude_OneToManyModel3 { id = m3.id }),
                    then => then.IncludeMany(m3 => m3.childs2.Take(2).Where(m4 => m4.model3333Id333 == m3.id).Select(m4 => new TestInclude_OneToManyModel4 { id = m4.id })))
                .Where(a => a.id <= model1.id)
                .ToList();
        }

        public class TestInclude_OneToManyModel11
        {
            [Column(IsIdentity = true)]
            public int id { get; set; }
            public int model2id { get; set; }
            public string m3setting { get; set; }
            public TestInclude_OneToManyModel22 model2 { get; set; }
            public string m1name { get; set; }
        }

        public class TestInclude_OneToManyModel22
        {
            [Column(IsIdentity = true)]
            public int id { get; set; }
            public string m2setting { get; set; }
            public string aaa { get; set; }
            public string bbb { get; set; }
            public List<TestInclude_OneToManyModel33> childs { get; set; }
        }
        public class TestInclude_OneToManyModel33
        {
            [Column(IsIdentity = true)]
            public int id { get; set; }
            public int model2Id { get; set; }
            public string title { get; set; }
            public string setting { get; set; }
        }
        [Fact]
        public void Include_OneToMany2()
        {
            string setting = "x";
            var model2 = new TestInclude_OneToManyModel22 { m2setting = DateTime.Now.Second.ToString(), aaa = "aaa" + DateTime.Now.Second, bbb = "bbb" + DateTime.Now.Second };
            model2.id = (int)g.sqlserver.Insert(model2).ExecuteIdentity();

            var model3s = new[]
            {
                new TestInclude_OneToManyModel33 {model2Id = model2.id, title = "testmodel3__111", setting = setting},
                new TestInclude_OneToManyModel33 {model2Id = model2.id, title = "testmodel3__222", setting = setting},
                new TestInclude_OneToManyModel33 {model2Id = model2.id, title = "testmodel3__333", setting = setting}
            };
            Assert.Equal(3, g.sqlserver.Insert(model3s).ExecuteAffrows());

            var model1 = new TestInclude_OneToManyModel11 { m1name = DateTime.Now.Second.ToString(), model2id = model2.id, m3setting = setting };
            model1.id = (int)g.sqlserver.Insert(model1).ExecuteIdentity();

            var t1 = g.sqlserver.Select<TestInclude_OneToManyModel11>()
                .LeftJoin(a => a.model2id == a.model2.id)
                .IncludeMany(a => a.model2.childs.Where(m3 => m3.model2Id == a.model2.id && m3.setting == a.m3setting))
                .Where(a => a.id <= model1.id)
                .ToList(true);

            var t11 = g.sqlserver.Select<TestInclude_OneToManyModel11>()
                .LeftJoin(a => a.model2id == a.model2.id)
                .IncludeMany(a => a.model2.childs.Take(1).Where(m3 => m3.model2Id == a.model2.id && m3.setting == a.m3setting))
                .Where(a => a.id <= model1.id)
                .ToList(true);

            //---- Select ----

            var at1 = g.sqlserver.Select<TestInclude_OneToManyModel11>()
                .LeftJoin(a => a.model2id == a.model2.id)
                .IncludeMany(a => a.model2.childs.Where(m3 => m3.model2Id == a.model2.id && m3.setting == a.m3setting).Select(m3 => new TestInclude_OneToManyModel33 { title = m3.title }))
                .Where(a => a.id <= model1.id)
                .ToList(true);

            var at11 = g.sqlserver.Select<TestInclude_OneToManyModel11>()
                .LeftJoin(a => a.model2id == a.model2.id)
                .IncludeMany(a => a.model2.childs.Take(1).Where(m3 => m3.model2Id == a.model2.id && m3.setting == a.m3setting).Select(m3 => new TestInclude_OneToManyModel33 { title = m3.title }))
                .Where(a => a.id <= model1.id)
                .ToList(true);
        }

        [Fact]
        public void Include_OneToChilds()
        {
            var tag1 = new Tag
            {
                Ddd = DateTime.Now.Second,
                Name = "test_oneToChilds_01_中国"
            };
            tag1.Id = (int)g.sqlserver.Insert(tag1).ExecuteIdentity();
            var tag1_1 = new Tag
            {
                Parent_id = tag1.Id,
                Ddd = DateTime.Now.Second,
                Name = "test_oneToChilds_01_北京"
            };
            tag1_1.Id = (int)g.sqlserver.Insert(tag1_1).ExecuteIdentity();
            var tag1_2 = new Tag
            {
                Parent_id = tag1.Id,
                Ddd = DateTime.Now.Second,
                Name = "test_oneToChilds_01_上海"
            };
            tag1_2.Id = (int)g.sqlserver.Insert(tag1_2).ExecuteIdentity();

            var tag2 = new Tag
            {
                Ddd = DateTime.Now.Second,
                Name = "test_oneToChilds_02_美国"
            };
            tag2.Id = (int)g.sqlserver.Insert(tag2).ExecuteIdentity();
            var tag2_1 = new Tag
            {
                Parent_id = tag2.Id,
                Ddd = DateTime.Now.Second,
                Name = "test_oneToChilds_02_纽约"
            };
            tag2_1.Id = (int)g.sqlserver.Insert(tag2_1).ExecuteIdentity();
            var tag2_2 = new Tag
            {
                Parent_id = tag2.Id,
                Ddd = DateTime.Now.Second,
                Name = "test_oneToChilds_02_华盛顿"
            };
            tag2_2.Id = (int)g.sqlserver.Insert(tag2_2).ExecuteIdentity();

            var tags0 = g.sqlserver.Select<Tag>()
                .Include(a => a.Parent)
                .Where(a => a.Id == tag1.Id || a.Id == tag2.Id)
                .ToList();

            var tags1 = g.sqlserver.Select<Tag>()
                .IncludeMany(a => a.Tags)
                .Include(a => a.Parent)
                .IncludeMany(a => a.Songs)
                .Where(a => a.Id == tag1.Id || a.Id == tag2.Id)
                .ToList();

            var tags2 = g.sqlserver.Select<Tag>()
                .IncludeMany(a => a.Tags,
                    then => then.Include(a => a.Parent).IncludeMany(a => a.Songs))
                .Include(a => a.Parent)
                .IncludeMany(a => a.Songs)
                .Where(a => a.Id == tag1.Id || a.Id == tag2.Id)
                .ToList();

            var tags3 = g.sqlserver.Select<Tag>()
                .IncludeMany(a => a.Tags,
                    then => then.Include(a => a.Parent).IncludeMany(a => a.Songs).IncludeMany(a => a.Tags))
                .Include(a => a.Parent)
                .IncludeMany(a => a.Songs)
                .Where(a => a.Id == tag1.Id || a.Id == tag2.Id)
                .ToList();

            var tags333 = g.sqlserver.Select<Tag>()
                .IncludeByPropertyName("Tags",
                    then => then.IncludeByPropertyName("Parent").IncludeByPropertyName("Songs").IncludeByPropertyName("Tags"))
                .IncludeByPropertyName("Parent")
                .IncludeByPropertyName("Songs")
                .Where(a => a.Id == tag1.Id || a.Id == tag2.Id)
                .ToList();
            var tags3339 = g.sqlserver.Select<Tag>()
                .Where(a => a.Id == tag1.Id || a.Id == tag2.Id)
                .ToList();
            tags3339
                .IncludeByPropertyName(g.sqlserver, "Tags",
                    then: then => then.IncludeByPropertyName("Parent").IncludeByPropertyName("Songs").IncludeByPropertyName("Tags"))
                .IncludeByPropertyName(g.sqlserver, "Parent")
                .IncludeByPropertyName(g.sqlserver, "Songs");

            var tags33331 = g.sqlserver.Select<object>()
                .AsType(typeof(Tag))
                .IncludeByPropertyName("Tags")
                .Where(a => (a as Tag).Id == tag1.Id || (a as Tag).Id == tag2.Id)
                .ToList();
            var tags333319 = g.sqlserver.Select<object>()
                .AsType(typeof(Tag))
                .Where(a => (a as Tag).Id == tag1.Id || (a as Tag).Id == tag2.Id)
                .ToList();
            tags333319.IncludeByPropertyName(g.sqlserver, "Tags");

            var tags3333 = g.sqlserver.Select<object>()
                .AsType(typeof(Tag))
                .IncludeByPropertyName("Tags",
                    then => then.IncludeByPropertyName("Parent").IncludeByPropertyName("Songs").IncludeByPropertyName("Tags"))
                .IncludeByPropertyName("Parent")
                .IncludeByPropertyName("Songs")
                .Where(a => (a as Tag).Id == tag1.Id || (a as Tag).Id == tag2.Id)
                .ToList();
            var tags33339 = g.sqlserver.Select<object>()
                .AsType(typeof(Tag))
                .Where(a => (a as Tag).Id == tag1.Id || (a as Tag).Id == tag2.Id)
                .ToList();
            tags33339
                .IncludeByPropertyName(g.sqlserver, "Tags",
                    then: then => then.IncludeByPropertyName("Parent").IncludeByPropertyName("Songs").IncludeByPropertyName("Tags"))
                .IncludeByPropertyName(g.sqlserver, "Parent")
                .IncludeByPropertyName(g.sqlserver, "Songs");

            var tags33333 = g.sqlserver.Select<object>()
                .AsType(typeof(Tag))
                .IncludeByPropertyName("Parent.Tags",
                    then => then.IncludeByPropertyName("Parent").IncludeByPropertyName("Songs").IncludeByPropertyName("Tags"))
                .IncludeByPropertyName("Songs")
                .Where(a => (a as Tag).Id == tag1_1.Id || (a as Tag).Id == tag1_1.Id)
                .ToList();
            var tags333339 = g.sqlserver.Select<object>()
                .AsType(typeof(Tag))
                .Where(a => (a as Tag).Id == tag1_1.Id || (a as Tag).Id == tag1_1.Id)
                .ToList();
            tags333339
                .IncludeByPropertyName(g.sqlserver, "Parent.Tags",
                    then: then => then.IncludeByPropertyName("Parent").IncludeByPropertyName("Songs").IncludeByPropertyName("Tags"))
                .IncludeByPropertyName(g.sqlserver, "Songs");

            var tags333333 = g.sqlserver.Select<object>()
                .AsType(typeof(Tag))
                .IncludeByPropertyName("Parent.Tags",
                    then => then.IncludeByPropertyName("Parent").IncludeByPropertyName("Songs").IncludeByPropertyName("Tags"))
                .IncludeByPropertyName("Songs")
                .Where(a => (a as Tag).Id == tag1.Id || (a as Tag).Id == tag2.Id)
                .ToList();
            var tags3333339 = g.sqlserver.Select<object>()
                .AsType(typeof(Tag))
                .Where(a => (a as Tag).Id == tag1.Id || (a as Tag).Id == tag2.Id)
                .ToList();
            tags3333339
                .IncludeByPropertyName(g.sqlserver, "Parent.Tags",
                    select: "Id, Name",
                    then: then => then.IncludeByPropertyName("Parent").IncludeByPropertyName("Songs").IncludeByPropertyName("Tags"))
                .IncludeByPropertyName(g.sqlserver, "Songs");
            var tags33333391 = g.sqlserver.Select<object>()
                .AsType(typeof(Tag))
                .Where(a => (a as Tag).Id == tag1_1.Id || (a as Tag).Id == tag2_1.Id)
                .ToList();
            tags33333391
                .IncludeByPropertyName(g.sqlserver, "Parent.Tags",
                    select: "Id, Name")
                .IncludeByPropertyName(g.sqlserver, "Songs");

            var tags11 = g.sqlserver.Select<Tag>()
                .IncludeMany(a => a.Tags.Take(1))
                .Include(a => a.Parent)
                .IncludeMany(a => a.Songs.Take(1))
                .Where(a => a.Id == tag1.Id || a.Id == tag2.Id)
                .ToList();

            var tags22 = g.sqlserver.Select<Tag>()
                .IncludeMany(a => a.Tags.Take(1),
                    then => then.Include(a => a.Parent).IncludeMany(a => a.Songs.Take(1)))
                .Include(a => a.Parent)
                .IncludeMany(a => a.Songs.Take(1))
                .Where(a => a.Id == tag1.Id || a.Id == tag2.Id)
                .ToList();

            var tags33 = g.sqlserver.Select<Tag>()
                .IncludeMany(a => a.Tags.Take(1),
                    then => then.Include(a => a.Parent).IncludeMany(a => a.Songs.Take(1)).IncludeMany(a => a.Tags.Take(1)))
                .Include(a => a.Parent)
                .IncludeMany(a => a.Songs.Take(1))
                .Where(a => a.Id == tag1.Id || a.Id == tag2.Id)
                .ToList();

            // --- Select ---

            var atags0 = g.sqlserver.Select<Tag>()
                .Include(a => a.Parent)
                .Where(a => a.Id == tag1.Id || a.Id == tag2.Id)
                .ToList();

            var atags1 = g.sqlserver.Select<Tag>()
                .IncludeMany(a => a.Tags.Select(b => new Tag { Id = b.Id, Name = b.Name }))
                .Include(a => a.Parent)
                .IncludeMany(a => a.Songs.Select(b => new Song { Id = b.Id, Title = b.Title }))
                .Where(a => a.Id == tag1.Id || a.Id == tag2.Id)
                .ToList();

            var atags2 = g.sqlserver.Select<Tag>()
                .IncludeMany(a => a.Tags.Select(b => new Tag { Id = b.Id, Name = b.Name }),
                    then => then.Include(a => a.Parent).IncludeMany(a => a.Songs))
                .Include(a => a.Parent)
                .IncludeMany(a => a.Songs.Select(b => new Song { Id = b.Id, Title = b.Title }))
                .Where(a => a.Id == tag1.Id || a.Id == tag2.Id)
                .ToList();

            var atags3 = g.sqlserver.Select<Tag>()
                .IncludeMany(a => a.Tags.Select(b => new Tag { Id = b.Id, Name = b.Name }),
                    then => then.Include(a => a.Parent).IncludeMany(a => a.Songs.Select(b => new Song { Id = b.Id, Title = b.Title })).IncludeMany(a => a.Tags.Select(b => new Tag { Id = b.Id, Name = b.Name })))
                .Include(a => a.Parent)
                .IncludeMany(a => a.Songs.Select(b => new Song { Id = b.Id, Title = b.Title }))
                .Where(a => a.Id == tag1.Id || a.Id == tag2.Id)
                .ToList();

            var atags11 = g.sqlserver.Select<Tag>()
                .IncludeMany(a => a.Tags.Take(1).Select(b => new Tag { Id = b.Id, Name = b.Name }))
                .Include(a => a.Parent)
                .IncludeMany(a => a.Songs.Take(1).Select(b => new Song { Id = b.Id, Title = b.Title }))
                .Where(a => a.Id == tag1.Id || a.Id == tag2.Id)
                .ToList();

            var atags22 = g.sqlserver.Select<Tag>()
                .IncludeMany(a => a.Tags.Take(1).Select(b => new Tag { Id = b.Id, Name = b.Name }),
                    then => then.Include(a => a.Parent).IncludeMany(a => a.Songs.Take(1).Select(b => new Song { Id = b.Id, Title = b.Title })))
                .Include(a => a.Parent)
                .IncludeMany(a => a.Songs.Take(1).Select(b => new Song { Id = b.Id, Title = b.Title }))
                .Where(a => a.Id == tag1.Id || a.Id == tag2.Id)
                .ToList();

            var atags33 = g.sqlserver.Select<Tag>()
                .IncludeMany(a => a.Tags.Take(1).Select(b => new Tag { Id = b.Id, Name = b.Name }),
                    then => then.Include(a => a.Parent).IncludeMany(a => a.Songs.Take(1).Select(b => new Song { Id = b.Id, Title = b.Title })).IncludeMany(a => a.Tags.Take(1).Select(b => new Tag { Id = b.Id, Name = b.Name })))
                .Include(a => a.Parent)
                .IncludeMany(a => a.Songs.Take(1).Select(b => new Song { Id = b.Id, Title = b.Title }))
                .Where(a => a.Id == tag1.Id || a.Id == tag2.Id)
                .ToList();
        }

        [Fact]
        public void Include_ManyToMany()
        {

            var tag1 = new Tag
            {
                Ddd = DateTime.Now.Second,
                Name = "test_manytoMany_01_中国"
            };
            tag1.Id = (int)g.sqlserver.Insert(tag1).ExecuteIdentity();
            var tag2 = new Tag
            {
                Ddd = DateTime.Now.Second,
                Name = "test_manytoMany_02_美国"
            };
            tag2.Id = (int)g.sqlserver.Insert(tag2).ExecuteIdentity();
            var tag3 = new Tag
            {
                Ddd = DateTime.Now.Second,
                Name = "test_manytoMany_03_日本"
            };
            tag3.Id = (int)g.sqlserver.Insert(tag3).ExecuteIdentity();

            var song1 = new Song
            {
                Create_time = DateTime.Now,
                Title = "test_manytoMany_01_我是中国人.mp3",
                Url = "http://ww.baidu.com/"
            };
            song1.Id = (int)g.sqlserver.Insert(song1).ExecuteIdentity();
            var song2 = new Song
            {
                Create_time = DateTime.Now,
                Title = "test_manytoMany_02_爱你一万年.mp3",
                Url = "http://ww.163.com/"
            };
            song2.Id = (int)g.sqlserver.Insert(song2).ExecuteIdentity();
            var song3 = new Song
            {
                Create_time = DateTime.Now,
                Title = "test_manytoMany_03_千年等一回.mp3",
                Url = "http://ww.sina.com/"
            };
            song3.Id = (int)g.sqlserver.Insert(song3).ExecuteIdentity();

            g.sqlserver.Insert(new Song_tag { Song_id = song1.Id, Tag_id = tag1.Id }).ExecuteAffrows();
            g.sqlserver.Insert(new Song_tag { Song_id = song2.Id, Tag_id = tag1.Id }).ExecuteAffrows();
            g.sqlserver.Insert(new Song_tag { Song_id = song3.Id, Tag_id = tag1.Id }).ExecuteAffrows();
            g.sqlserver.Insert(new Song_tag { Song_id = song1.Id, Tag_id = tag2.Id }).ExecuteAffrows();
            g.sqlserver.Insert(new Song_tag { Song_id = song3.Id, Tag_id = tag2.Id }).ExecuteAffrows();
            g.sqlserver.Insert(new Song_tag { Song_id = song3.Id, Tag_id = tag3.Id }).ExecuteAffrows();

            var songs1 = g.sqlserver.Select<Song>()
                .IncludeMany(a => a.Tags)
                .Where(a => a.Id == song1.Id || a.Id == song2.Id || a.Id == song3.Id)
                .ToList();
            Assert.Equal(3, songs1.Count);
            Assert.Equal(2, songs1[0].Tags.Count);
            Assert.Equal(1, songs1[1].Tags.Count);
            Assert.Equal(3, songs1[2].Tags.Count);

            var songs2 = g.sqlserver.Select<Song>()
                .IncludeMany(a => a.Tags,
                    then => then.IncludeMany(t => t.Songs))
                .Where(a => a.Id == song1.Id || a.Id == song2.Id || a.Id == song3.Id)
                .ToList();
            Assert.Equal(3, songs2.Count);
            Assert.Equal(2, songs2[0].Tags.Count);
            Assert.Equal(1, songs2[1].Tags.Count);
            Assert.Equal(3, songs2[2].Tags.Count);

            var tags3 = g.sqlserver.Select<Song_tag>()
                .Include(a => a.Tag.Parent)
                .IncludeMany(a => a.Tag.Songs)
                .Where(a => a.Tag.Id == tag1.Id || a.Tag.Id == tag2.Id)
                .ToList(true);


            var songs11 = g.sqlserver.Select<Song>()
                .IncludeMany(a => a.Tags.Take(1))
                .Where(a => a.Id == song1.Id || a.Id == song2.Id || a.Id == song3.Id)
                .ToList();
            Assert.Equal(3, songs11.Count);
            Assert.Equal(1, songs11[0].Tags.Count);
            Assert.Equal(1, songs11[1].Tags.Count);
            Assert.Equal(1, songs11[2].Tags.Count);

            var songs22 = g.sqlserver.Select<Song>()
                .IncludeMany(a => a.Tags.Take(1),
                    then => then.IncludeMany(t => t.Songs.Take(1)))
                .Where(a => a.Id == song1.Id || a.Id == song2.Id || a.Id == song3.Id)
                .ToList();
            Assert.Equal(3, songs22.Count);
            Assert.Equal(1, songs22[0].Tags.Count);
            Assert.Equal(1, songs22[1].Tags.Count);
            Assert.Equal(1, songs22[2].Tags.Count);

            var tags33 = g.sqlserver.Select<Song_tag>()
                .Include(a => a.Tag.Parent)
                .IncludeMany(a => a.Tag.Songs.Take(1))
                .Where(a => a.Tag.Id == tag1.Id || a.Tag.Id == tag2.Id)
                .ToList(true);

            // --- Select ---

            new List<Song>(new[] { song1, song2, song3 }).IncludeMany(g.sqlserver, a => a.Tags.Select(b => new Tag { Id = b.Id, Name = b.Name }));

            var asongs1 = g.sqlserver.Select<Song>()
                .IncludeMany(a => a.Tags.Select(b => new Tag { Id = b.Id, Name = b.Name }))
                .Where(a => a.Id == song1.Id || a.Id == song2.Id || a.Id == song3.Id)
                .ToList();
            Assert.Equal(3, songs1.Count);
            Assert.Equal(2, songs1[0].Tags.Count);
            Assert.Equal(1, songs1[1].Tags.Count);
            Assert.Equal(3, songs1[2].Tags.Count);

            var asongs2 = g.sqlserver.Select<Song>()
                .IncludeMany(a => a.Tags.Select(b => new Tag { Id = b.Id, Name = b.Name }),
                    then => then.IncludeMany(t => t.Songs.Select(b => new Song { Id = b.Id, Title = b.Title })))
                .Where(a => a.Id == song1.Id || a.Id == song2.Id || a.Id == song3.Id)
                .ToList();
            Assert.Equal(3, songs2.Count);
            Assert.Equal(2, songs2[0].Tags.Count);
            Assert.Equal(1, songs2[1].Tags.Count);
            Assert.Equal(3, songs2[2].Tags.Count);

            var atags3 = g.sqlserver.Select<Song_tag>()
                .Include(a => a.Tag.Parent)
                .IncludeMany(a => a.Tag.Songs.Select(b => new Song { Id = b.Id, Title = b.Title }))
                .Where(a => a.Tag.Id == tag1.Id || a.Tag.Id == tag2.Id)
                .ToList(true);


            var asongs11 = g.sqlserver.Select<Song>()
                .IncludeMany(a => a.Tags.Take(1).Select(b => new Tag { Id = b.Id, Name = b.Name }))
                .Where(a => a.Id == song1.Id || a.Id == song2.Id || a.Id == song3.Id)
                .ToList();
            Assert.Equal(3, songs11.Count);
            Assert.Equal(1, songs11[0].Tags.Count);
            Assert.Equal(1, songs11[1].Tags.Count);
            Assert.Equal(1, songs11[2].Tags.Count);

            var asongs22 = g.sqlserver.Select<Song>()
                .IncludeMany(a => a.Tags.Take(1).Select(b => new Tag { Id = b.Id, Name = b.Name }),
                    then => then.IncludeMany(t => t.Songs.Take(1).Select(b => new Song { Id = b.Id, Title = b.Title })))
                .Where(a => a.Id == song1.Id || a.Id == song2.Id || a.Id == song3.Id)
                .ToList();
            Assert.Equal(3, songs22.Count);
            Assert.Equal(1, songs22[0].Tags.Count);
            Assert.Equal(1, songs22[1].Tags.Count);
            Assert.Equal(1, songs22[2].Tags.Count);

            var atags33 = g.sqlserver.Select<Song_tag>()
                .Include(a => a.Tag.Parent)
                .IncludeMany(a => a.Tag.Songs.Take(1).Select(b => new Song { Id = b.Id, Title = b.Title }))
                .Where(a => a.Tag.Id == tag1.Id || a.Tag.Id == tag2.Id)
                .ToList(true);
        }

        public class ToDel1Pk
        {
            [Column(IsIdentity = true)]
            public int id { get; set; }
            public string name { get; set; }
        }
        public class ToDel2Pk
        {
            [Column(IsPrimary = true)]
            public Guid pk1 { get; set; }
            [Column(IsPrimary = true)]
            public string pk2 { get; set; }
            public string name { get; set; }
        }
        public class ToDel3Pk
        {
            [Column(IsPrimary = true)]
            public Guid pk1 { get; set; }
            [Column(IsPrimary = true)]
            public int pk2 { get; set; }
            [Column(IsPrimary = true)]
            public string pk3 { get; set; }
            public string name { get; set; }
        }
        [Fact]
        public void ToDelete()
        {
            g.sqlserver.Select<ToDel1Pk>().ToDelete().ExecuteAffrows();
            Assert.Equal(0, g.sqlserver.Select<ToDel1Pk>().Count());
            g.sqlserver.Insert(new[] {
                new ToDel1Pk{ name = "name1"},
                new ToDel1Pk{ name = "name2"},
                new ToDel1Pk{ name = "nick1"},
                new ToDel1Pk{ name = "nick2"},
                new ToDel1Pk{ name = "nick3"}
            }).ExecuteAffrows();
            Assert.Equal(2, g.sqlserver.Select<ToDel1Pk>().Where(a => a.name.StartsWith("name")).ToDelete().ExecuteAffrows());
            Assert.Equal(3, g.sqlserver.Select<ToDel1Pk>().Count());
            Assert.Equal(3, g.sqlserver.Select<ToDel1Pk>().Where(a => a.name.StartsWith("nick")).Count());

            g.sqlserver.Select<ToDel2Pk>().ToDelete().ExecuteAffrows();
            Assert.Equal(0, g.sqlserver.Select<ToDel2Pk>().Count());
            g.sqlserver.Insert(new[] {
                new ToDel2Pk{ pk1 = FreeUtil.NewMongodbId(), pk2 = "pk2", name = "name1"},
                new ToDel2Pk{ pk1 = FreeUtil.NewMongodbId(), pk2 = "pk2", name = "name2"},
                new ToDel2Pk{ pk1 = FreeUtil.NewMongodbId(), pk2 = "pk2", name = "nick1"},
                new ToDel2Pk{ pk1 = FreeUtil.NewMongodbId(), pk2 = "pk2", name = "nick2"},
                new ToDel2Pk{ pk1 = FreeUtil.NewMongodbId(), pk2 = "pk2", name = "nick3"}
            }).ExecuteAffrows();
            Assert.Equal(2, g.sqlserver.Select<ToDel2Pk>().Where(a => a.name.StartsWith("name")).ToDelete().ExecuteAffrows());
            Assert.Equal(3, g.sqlserver.Select<ToDel2Pk>().Count());
            Assert.Equal(3, g.sqlserver.Select<ToDel2Pk>().Where(a => a.name.StartsWith("nick")).Count());

            g.sqlserver.Select<ToDel3Pk>().ToDelete().ExecuteAffrows();
            Assert.Equal(0, g.sqlserver.Select<ToDel3Pk>().Count());
            g.sqlserver.Insert(new[] {
                new ToDel3Pk{ pk1 = FreeUtil.NewMongodbId(), pk2 = 1, pk3 = "pk3", name = "name1"},
                new ToDel3Pk{ pk1 = FreeUtil.NewMongodbId(), pk2 = 1, pk3 = "pk3", name = "name2"},
                new ToDel3Pk{ pk1 = FreeUtil.NewMongodbId(), pk2 = 1, pk3 = "pk3", name = "nick1"},
                new ToDel3Pk{ pk1 = FreeUtil.NewMongodbId(), pk2 = 1, pk3 = "pk3", name = "nick2"},
                new ToDel3Pk{ pk1 = FreeUtil.NewMongodbId(), pk2 = 1, pk3 = "pk3", name = "nick3"}
            }).ExecuteAffrows();
            Assert.Equal(2, g.sqlserver.Select<ToDel3Pk>().Where(a => a.name.StartsWith("name")).ToDelete().ExecuteAffrows());
            Assert.Equal(3, g.sqlserver.Select<ToDel3Pk>().Count());
            Assert.Equal(3, g.sqlserver.Select<ToDel3Pk>().Where(a => a.name.StartsWith("nick")).Count());
        }

        public class ToUpd1Pk
        {
            [Column(IsIdentity = true)]
            public int id { get; set; }
            public string name { get; set; }
        }
        public class ToUpd2Pk
        {
            [Column(IsPrimary = true)]
            public Guid pk1 { get; set; }
            [Column(IsPrimary = true)]
            public string pk2 { get; set; }
            public string name { get; set; }
        }
        public class ToUpd3Pk
        {
            [Column(IsPrimary = true)]
            public Guid pk1 { get; set; }
            [Column(IsPrimary = true)]
            public int pk2 { get; set; }
            [Column(IsPrimary = true)]
            public string pk3 { get; set; }
            public string name { get; set; }
        }
        [Fact]
        public void ToUpdate()
        {
            g.sqlserver.Select<ToUpd1Pk>().ToDelete().ExecuteAffrows();
            Assert.Equal(0, g.sqlserver.Select<ToUpd1Pk>().Count());
            g.sqlserver.Insert(new[] {
                new ToUpd1Pk{ name = "name1"},
                new ToUpd1Pk{ name = "name2"},
                new ToUpd1Pk{ name = "nick1"},
                new ToUpd1Pk{ name = "nick2"},
                new ToUpd1Pk{ name = "nick3"}
            }).ExecuteAffrows();
            Assert.Equal(2, g.sqlserver.Select<ToUpd1Pk>().Where(a => a.name.StartsWith("name")).ToUpdate().Set(a => a.name, "nick?").ExecuteAffrows());
            Assert.Equal(5, g.sqlserver.Select<ToUpd1Pk>().Count());
            Assert.Equal(5, g.sqlserver.Select<ToUpd1Pk>().Where(a => a.name.StartsWith("nick")).Count());

            g.sqlserver.Select<ToUpd2Pk>().ToDelete().ExecuteAffrows();
            Assert.Equal(0, g.sqlserver.Select<ToUpd2Pk>().Count());
            g.sqlserver.Insert(new[] {
                new ToUpd2Pk{ pk1 = FreeUtil.NewMongodbId(), pk2 = "pk2", name = "name1"},
                new ToUpd2Pk{ pk1 = FreeUtil.NewMongodbId(), pk2 = "pk2", name = "name2"},
                new ToUpd2Pk{ pk1 = FreeUtil.NewMongodbId(), pk2 = "pk2", name = "nick1"},
                new ToUpd2Pk{ pk1 = FreeUtil.NewMongodbId(), pk2 = "pk2", name = "nick2"},
                new ToUpd2Pk{ pk1 = FreeUtil.NewMongodbId(), pk2 = "pk2", name = "nick3"}
            }).ExecuteAffrows();
            Assert.Equal(2, g.sqlserver.Select<ToUpd2Pk>().Where(a => a.name.StartsWith("name")).ToUpdate().Set(a => a.name, "nick?").ExecuteAffrows());
            Assert.Equal(5, g.sqlserver.Select<ToUpd2Pk>().Count());
            Assert.Equal(5, g.sqlserver.Select<ToUpd2Pk>().Where(a => a.name.StartsWith("nick")).Count());

            g.sqlserver.Select<ToUpd3Pk>().ToDelete().ExecuteAffrows();
            Assert.Equal(0, g.sqlserver.Select<ToUpd3Pk>().Count());
            g.sqlserver.Insert(new[] {
                new ToUpd3Pk{ pk1 = FreeUtil.NewMongodbId(), pk2 = 1, pk3 = "pk3", name = "name1"},
                new ToUpd3Pk{ pk1 = FreeUtil.NewMongodbId(), pk2 = 1, pk3 = "pk3", name = "name2"},
                new ToUpd3Pk{ pk1 = FreeUtil.NewMongodbId(), pk2 = 1, pk3 = "pk3", name = "nick1"},
                new ToUpd3Pk{ pk1 = FreeUtil.NewMongodbId(), pk2 = 1, pk3 = "pk3", name = "nick2"},
                new ToUpd3Pk{ pk1 = FreeUtil.NewMongodbId(), pk2 = 1, pk3 = "pk3", name = "nick3"}
            }).ExecuteAffrows();
            Assert.Equal(2, g.sqlserver.Select<ToUpd3Pk>().Where(a => a.name.StartsWith("name")).ToUpdate().Set(a => a.name, "nick?").ExecuteAffrows());
            Assert.Equal(5, g.sqlserver.Select<ToUpd3Pk>().Count());
            Assert.Equal(5, g.sqlserver.Select<ToUpd3Pk>().Where(a => a.name.StartsWith("nick")).Count());
        }

        [Fact]
        public void WithLock()
        {
            var orm = g.sqlserver;
            orm.Transaction(() =>
            {
                var sql = orm.Select<ToUpd1Pk>().WithLock().Limit(1).ToSql().Replace("\r\n", "");
                Assert.Equal("SELECT TOP 1 a.[id], a.[name] FROM [ToUpd1Pk] a With(NoLock)", sql);
                orm.Select<ToUpd1Pk>().WithLock().Limit(1).ToList();

                sql = orm.Select<ToUpd1Pk>().WithLock(SqlServerLock.NoLock).Limit(1).ToSql().Replace("\r\n", "");
                Assert.Equal("SELECT TOP 1 a.[id], a.[name] FROM [ToUpd1Pk] a With(NoLock)", sql);
                orm.Select<ToUpd1Pk>().WithLock(SqlServerLock.NoLock).Limit(1).ToList();

                sql = orm.Select<ToUpd1Pk>().WithLock(SqlServerLock.NoLock | SqlServerLock.NoWait).Limit(1).ToSql().Replace("\r\n", "");
                Assert.Equal("SELECT TOP 1 a.[id], a.[name] FROM [ToUpd1Pk] a With(NoLock, NoWait)", sql);
                orm.Select<ToUpd1Pk>().WithLock(SqlServerLock.NoLock | SqlServerLock.NoWait).Limit(1).ToList();

                sql = orm.Select<ToUpd1Pk>().WithLock(SqlServerLock.UpdLock | SqlServerLock.RowLock | SqlServerLock.NoWait).Limit(1).ToSql().Replace("\r\n", "");
                Assert.Equal("SELECT TOP 1 a.[id], a.[name] FROM [ToUpd1Pk] a With(UpdLock, RowLock, NoWait)", sql);
                orm.Select<ToUpd1Pk>().WithLock(SqlServerLock.UpdLock | SqlServerLock.RowLock | SqlServerLock.NoWait).Limit(1).ToList();
            });

            var sql2 = orm.Select<Topic>()
                .InnerJoin(a => a.Type.Guid == a.Id)
                .WithLock()
                .ToSql();
            Assert.Equal(@"SELECT a.[Id], a.[Clicks], a.[TypeGuid], a__Type.[Guid], a__Type.[ParentId], a__Type.[Name], a.[Title], a.[CreateTime] 
FROM [tb_topic22] a With(NoLock) 
INNER JOIN [TestTypeInfo] a__Type With(NoLock) ON a__Type.[Guid] = a.[Id]", sql2);

            sql2 = orm.Select<Topic>()
                .InnerJoin(a => a.Type.Guid == a.Id)
                .WithLock(SqlServerLock.NoLock, new Dictionary<Type, bool>
                {
                    [typeof(TestTypeInfo)] = true
                })
                .ToSql();
            Assert.Equal(@"SELECT a.[Id], a.[Clicks], a.[TypeGuid], a__Type.[Guid], a__Type.[ParentId], a__Type.[Name], a.[Title], a.[CreateTime] 
FROM [tb_topic22] a 
INNER JOIN [TestTypeInfo] a__Type With(NoLock) ON a__Type.[Guid] = a.[Id]", sql2);

            sql2 = orm.Select<Topic>()
                .InnerJoin(a => a.Type.Guid == a.Id)
                .WithLock(SqlServerLock.NoLock)
                .ToSql();
            Assert.Equal(@"SELECT a.[Id], a.[Clicks], a.[TypeGuid], a__Type.[Guid], a__Type.[ParentId], a__Type.[Name], a.[Title], a.[CreateTime] 
FROM [tb_topic22] a With(NoLock) 
INNER JOIN [TestTypeInfo] a__Type With(NoLock) ON a__Type.[Guid] = a.[Id]", sql2);

            sql2 = orm.Select<Topic>()
                .InnerJoin(a => a.Type.Guid == a.Id)
                .WithLock(SqlServerLock.NoLock)
                .Where(a => orm.Select<TestTypeInfo>()
                    .WithLock(SqlServerLock.NoLock, null).Where(b => b.Guid == a.TypeGuid).Any())
                .ToSql();
            Assert.Equal(@"SELECT a.[Id], a.[Clicks], a.[TypeGuid], a__Type.[Guid], a__Type.[ParentId], a__Type.[Name], a.[Title], a.[CreateTime] 
FROM [tb_topic22] a With(NoLock) 
INNER JOIN [TestTypeInfo] a__Type With(NoLock) ON a__Type.[Guid] = a.[Id] 
WHERE (exists(SELECT TOP 1 1 
    FROM [TestTypeInfo] b With(NoLock) 
    WHERE (b.[Guid] = a.[TypeGuid])))", sql2);
        }
        [Fact]
        public void ForUpdate()
        {
            var orm = g.sqlserver;

            Assert.Equal("安全起见，请务必在事务开启之后，再使用 ForUpdate",
                Assert.Throws<Exception>(() => orm.Select<ToUpd1Pk>().ForUpdate().Limit(1).ToList())?.Message);

            orm.Transaction(() =>
            {
                var sql = orm.Select<ToUpd1Pk>().ForUpdate().Limit(1).ToSql().Replace("\r\n", "");
                Assert.Equal("SELECT TOP 1 a.[id], a.[name] FROM [ToUpd1Pk] a With(UpdLock, RowLock)", sql);
                orm.Select<ToUpd1Pk>().ForUpdate().Limit(1).ToList();

                sql = orm.Select<ToUpd1Pk>().ForUpdate(true).Limit(1).ToSql().Replace("\r\n", "");
                Assert.Equal("SELECT TOP 1 a.[id], a.[name] FROM [ToUpd1Pk] a With(UpdLock, RowLock, NoWait)", sql);
                orm.Select<ToUpd1Pk>().ForUpdate(true).Limit(1).ToList();
            });
        }

        [Fact]
        public void ToTreeList()
        {
            var fsql = g.sqlserver;
            fsql.Delete<BaseDistrict>().Where("1=1").ExecuteAffrows();
            var repo = fsql.GetRepository<VM_District_Child>();
            repo.DbContextOptions.EnableCascadeSave = true;
            repo.DbContextOptions.NoneParameter = true;
            repo.Insert(new VM_District_Child
            {
                Code = "100000",
                Name = "中国",
                Childs = new List<VM_District_Child>(new[] {
                    new VM_District_Child
                    {
                        Code = "110000",
                        Name = "北京",
                        Childs = new List<VM_District_Child>(new[] {
                            new VM_District_Child{ Code="110100", Name = "北京市" },
                            new VM_District_Child{ Code="110101", Name = "东城区" },
                        })
                    }
                })
            });

            var t1 = fsql.Select<VM_District_Parent>()
                .InnerJoin(a => a.ParentCode == a.Parent.Code)
                .Where(a => a.Code == "110101")
                .ToList(true);
            Assert.Single(t1);
            Assert.Equal("110101", t1[0].Code);
            Assert.NotNull(t1[0].Parent);
            Assert.Equal("110000", t1[0].Parent.Code);

            var t2 = fsql.Select<VM_District_Parent>()
                .InnerJoin(a => a.ParentCode == a.Parent.Code)
                .InnerJoin(a => a.Parent.ParentCode == a.Parent.Parent.Code)
                .Where(a => a.Code == "110101")
                .ToList(true);
            Assert.Single(t2);
            Assert.Equal("110101", t2[0].Code);
            Assert.NotNull(t2[0].Parent);
            Assert.Equal("110000", t2[0].Parent.Code);
            Assert.NotNull(t2[0].Parent.Parent);
            Assert.Equal("100000", t2[0].Parent.Parent.Code);

            var t3 = fsql.Select<VM_District_Child>().ToTreeList();
            Assert.Single(t3);
            Assert.Equal("100000", t3[0].Code);
            Assert.Single(t3[0].Childs);
            Assert.Equal("110000", t3[0].Childs[0].Code);
            Assert.Equal(2, t3[0].Childs[0].Childs.Count);
            Assert.Equal("110100", t3[0].Childs[0].Childs[0].Code);
            Assert.Equal("110101", t3[0].Childs[0].Childs[1].Code);

            t3 = fsql.Select<VM_District_Child>().Where(a => a.Name == "中国").AsTreeCte().OrderBy(a => a.Code).ToTreeList();
            Assert.Single(t3);
            Assert.Equal("100000", t3[0].Code);
            Assert.Single(t3[0].Childs);
            Assert.Equal("110000", t3[0].Childs[0].Code);
            Assert.Equal(2, t3[0].Childs[0].Childs.Count);
            Assert.Equal("110100", t3[0].Childs[0].Childs[0].Code);
            Assert.Equal("110101", t3[0].Childs[0].Childs[1].Code);

            t3 = fsql.Select<VM_District_Child>().Where(a => a.Name == "中国").AsTreeCte().OrderBy(a => a.Code).ToList();
            Assert.Equal(4, t3.Count);
            Assert.Equal("100000", t3[0].Code);
            Assert.Equal("110000", t3[1].Code);
            Assert.Equal("110100", t3[2].Code);
            Assert.Equal("110101", t3[3].Code);

            t3 = fsql.Select<VM_District_Child>().Where(a => a.Name == "北京").AsTreeCte().OrderBy(a => a.Code).ToList();
            Assert.Equal(3, t3.Count);
            Assert.Equal("110000", t3[0].Code);
            Assert.Equal("110100", t3[1].Code);
            Assert.Equal("110101", t3[2].Code);

            var t4 = fsql.Select<VM_District_Child>().Where(a => a.Name == "中国").AsTreeCte(a => a.Name).OrderBy(a => a.Code)
                .ToList(a => new { item = a, level = Convert.ToInt32("a.cte_level"), path = "a.cte_path" });
            Assert.Equal(4, t4.Count);
            Assert.Equal("100000", t4[0].item.Code);
            Assert.Equal("110000", t4[1].item.Code);
            Assert.Equal("110100", t4[2].item.Code);
            Assert.Equal("110101", t4[3].item.Code);
            Assert.Equal("中国", t4[0].path);
            Assert.Equal("中国 -> 北京", t4[1].path);
            Assert.Equal("中国 -> 北京 -> 北京市", t4[2].path);
            Assert.Equal("中国 -> 北京 -> 东城区", t4[3].path);

            t4 = fsql.Select<VM_District_Child>().Where(a => a.Name == "中国").AsTreeCte(a => a.Name + "[" + a.Code + "]").OrderBy(a => a.Code)
                .ToList(a => new { item = a, level = Convert.ToInt32("a.cte_level"), path = "a.cte_path" });
            Assert.Equal(4, t4.Count);
            Assert.Equal("100000", t4[0].item.Code);
            Assert.Equal("110000", t4[1].item.Code);
            Assert.Equal("110100", t4[2].item.Code);
            Assert.Equal("110101", t4[3].item.Code);
            Assert.Equal("中国[100000]", t4[0].path);
            Assert.Equal("中国[100000] -> 北京[110000]", t4[1].path);
            Assert.Equal("中国[100000] -> 北京[110000] -> 北京市[110100]", t4[2].path);
            Assert.Equal("中国[100000] -> 北京[110000] -> 东城区[110101]", t4[3].path);

            var select = fsql.Select<VM_District_Child>()
                .Where(a => a.Name == "中国")
                .AsTreeCte()
                //.OrderBy("a.cte_level desc") //递归层级
                ;
            // var list = select.ToList(); //自己调试看查到的数据
            select.ToUpdate().Set(a => a.testint, 855).ExecuteAffrows();
            Assert.Equal(855, fsql.Select<VM_District_Child>()
                .Where(a => a.Name == "中国")
                .AsTreeCte().Distinct().First(a => a.testint));

            Assert.Equal(4, select.ToDelete().ExecuteAffrows());
            Assert.False(fsql.Select<VM_District_Child>()
                .Where(a => a.Name == "中国")
                .AsTreeCte().Any());
        }

        [Table(Name = "D_District")]
        public class BaseDistrict
        {
            [Column(IsPrimary = true, StringLength = 6)]
            public string Code { get; set; }

            [Column(StringLength = 20, IsNullable = false)]
            public string Name { get; set; }

            [Column(StringLength = 6)]
            public virtual string ParentCode { get; set; }

            public int testint { get; set; }
        }
        [Table(Name = "D_District", DisableSyncStructure = true)]
        public class VM_District_Child : BaseDistrict
        {
            public override string ParentCode { get => base.ParentCode; set => base.ParentCode = value; }

            [Navigate(nameof(ParentCode))]
            public List<VM_District_Child> Childs { get; set; }
        }
        [Table(Name = "D_District", DisableSyncStructure = true)]
        public class VM_District_Parent : BaseDistrict
        {
            public override string ParentCode { get => base.ParentCode; set => base.ParentCode = value; }

            [Navigate(nameof(ParentCode))]
            public VM_District_Parent Parent { get; set; }
        }
    }
}
