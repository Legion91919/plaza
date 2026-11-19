# Product brief

> **Showcase project.** Buurt Forum (fictional), a federation of 120 neighbourhood associations is a fictional client and every name, user and number here is invented.
> Plaza is an original community social network, built to show how the Baton model handles a consumer-scale product.

## Client
Buurt Forum (fictional), a federation of 120 neighbourhood associations

## Problem
Neighbourhood associations run on scattered chat groups and email lists, so notices get lost, events are missed and nobody knows who actually lives in the area.

## Users
Residents (daily, mobile); group moderators (weekly); association admins (monthly).

## Outcomes
Safety alerts reach every member of a group within 10 seconds; 80% of an association's households verified in 3 months.

## Constraints
Addresses visible only to verified neighbours in the same group; moderators can remove posts with an audit trail.

## Out of scope (first release)
Anything not needed for the outcomes above in the first three slices.

## Traits (for the architecture advisor)
- ReadHeavy: "residents read notices and feeds far more than they post"
- PersonalData: "verified home addresses, phone numbers, profiles"
- SeveralBusinessAreas: "profiles and connections, groups, feed, events, notifications"
- LiveTracking: "live notifications for safety alerts and replies"
- SmallTeam: "four engineers"

Global-scale profile used for the evolution plan: ReadHeavy, PersonalData, SeveralBusinessAreas, LiveTracking, GlobalScale.
