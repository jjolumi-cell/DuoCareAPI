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
                var senderLocation = await context.UserLocations
                    .Where(l => l.UserId == appointment.SenderId)
                    .OrderByDescending(l => l.Timestamp)
                    .FirstOrDefaultAsync();

                var receiverLocation = await context.UserLocations
                    .Where(l => l.UserId == appointment.ReceiverId)
                    .OrderByDescending(l => l.Timestamp)
                    .FirstOrDefaultAsync();

                double senderDistance = CalculateDistance(
                    senderLocation?.Latitude,
                    senderLocation?.Longitude,
                    appointment.Latitude,
                    appointment.Longitude
                );

                double receiverDistance = CalculateDistance(
                    receiverLocation?.Latitude,
                    receiverLocation?.Longitude,
                    appointment.Latitude,
                    appointment.Longitude
                );

                if (senderDistance > 50)
                {
                    appointment.AbsentUserId = appointment.SenderId;
                    appointment.AbsentUserLatitude = senderLocation?.Latitude;
                    appointment.AbsentUserLongitude = senderLocation?.Longitude;
                    appointment.AbsentUserDistance = senderDistance;
                }

                if (receiverDistance > 50)
                {
                    appointment.AbsentUserId = appointment.ReceiverId;
                    appointment.AbsentUserLatitude = receiverLocation?.Latitude;
                    appointment.AbsentUserLongitude = receiverLocation?.Longitude;
                    appointment.AbsentUserDistance = receiverDistance;
                }

                appointment.Status = AppointmentStatus.Cancelado;
                appointment.AutoCancelledAt = DateTime.Now;
            }

            await context.SaveChangesAsync();
        }


        private double CalculateDistance(double? lat1, double? lon1, double lat2, double lon2)
        {
            if (lat1 == null || lon1 == null)
                return double.MaxValue;

            double R = 6371000; // metros
            double dLat = (lat2 - lat1.Value) * Math.PI / 180;
            double dLon = (lon2 - lon1.Value) * Math.PI / 180;

            double a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1.Value * Math.PI / 180) *
                Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }
    }
}
