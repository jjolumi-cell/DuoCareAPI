using DuoCare.Data;
using DuoCare.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;


namespace DuoCare.Services
{
    public class AppointmentCancelService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public AppointmentCancelService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken); 
                await CheckExpiredAppointments();
            }
        }

        private async Task CheckExpiredAppointments()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var now = DateTime.Now;

            var expiredAppointments = await context.Appointments
                .Where(a =>
                    a.Date < now &&
                    a.Status != AppointmentStatus.Completado &&
                    a.Status != AppointmentStatus.Cancelado)
                .ToListAsync();

            if (expiredAppointments.Count == 0)
                return;

            foreach (var a in expiredAppointments)
            {
                a.Status = AppointmentStatus.Cancelado;
            }

            await context.SaveChangesAsync();
        }
    }
}
