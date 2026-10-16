using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public static class QueryExtensions
{
    public static IQueryable<Enrolment> WithDetails(this IQueryable<Enrolment> query) =>
        query.Include(e => e.Participant).Include(e => e.Event).Include(e => e.Category);

    public static IQueryable<RaceResult> WithDetails(this IQueryable<RaceResult> query) =>
        query.Include(r => r.Enrolment).ThenInclude(e => e.Participant)
             .Include(r => r.Enrolment).ThenInclude(e => e.Event)
             .Include(r => r.Enrolment).ThenInclude(e => e.Category);
}