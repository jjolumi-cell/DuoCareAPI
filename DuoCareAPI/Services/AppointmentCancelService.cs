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
                    a.Date.AddMinutes(20) < now &&
                    a.Status != AppointmentStatus.Completado &&
                    a.Status != AppointmentStatus.Cancelado)
                .ToListAsync();

            if (!expiredAppointments.Any())
                return;

            foreach (var appointment in expiredAppointments)
            {
                appointment.AbsentUserId = appointment.SenderId;
                appointment.AbsentUserLatitude = null;
                appointment.AbsentUserLongitude = null;
                appointment.AbsentUserDistance = double.MaxValue;

                appointment.Status = AppointmentStatus.Cancelado;
                appointment.AutoCancelledAt = DateTime.Now;
            }

            await context.SaveChangesAsync();
        }
    }
}

