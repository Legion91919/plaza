# 0001. Architecture style

- Status: Accepted
- Date: 2026-11-19

## Context

Neighbourhood associations run on scattered chat groups and email lists, so notices get lost, events are missed and nobody knows who actually lives in the area. Launch traits: ReadHeavy, PersonalData, SeveralBusinessAreas, LiveTracking, SmallTeam. At global scale the advisor recommends Microservices.

## Decision

Launch as a modular monolith (Profiles, Groups, Feed, Events, Notifications) with SignalR for live notifications. Module boundaries match the services the advisor proposes at global scale. Evolution trigger: Extract Notifications first when live connections exceed what two app instances with a SignalR backplane hold (~50k), then Feed when group feeds dominate database load.

## Alternatives considered

- Microservices now (reference #20 at global scale): five deployables for four engineers.
- A plain CRUD app without module boundaries: fast to start, but notifications and moderation would tangle through every feature.

## Consequences

- Module boundaries match the future services, so extraction is a deployment change, not a rewrite.
- One deployable and one database for the launch team to run.
