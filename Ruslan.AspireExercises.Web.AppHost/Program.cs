var builder = DistributedApplication.CreateBuilder(args);

var username = builder.AddParameter("database-username");
var password = builder.AddParameter("database-password", secret: true);

var database = builder.AddPostgres("postgres", username, password)
    .WithLifetime(ContainerLifetime.Persistent)//оставляет контейнер бд после остановки приложения
    .WithPgAdmin(options =>
    {
        options.WithHostPort(5050);//чтобы pgadmin висел постоянно на порту 5050
        options.WithLifetime(ContainerLifetime.Persistent);
    })
    .WithDataVolume()//сохраняет данные в бд после остановки приложения
    .AddDatabase("database");

var migrator = builder.AddProject<Projects.MigrationService>("migrator")
    .WithReference(database)
    .WaitFor(database);// перед запуском проекта web дожидается пока не запустится бд

builder.AddProject<Projects.Ruslan_AspireExercises_Web>("web")
    .WithReference(database)
    .WaitForCompletion(migrator); // перед запуском проекта web дожидается пока не закончит свою работу migrator



builder.Build().Run();
