# Godot client stub

This project reserves the future client boundary and references only `Peloton.Application`.

Milestone 0 contains no Godot SDK dependency, scenes, nodes, HQ UI, cards, or race visualization. Headless tests and `Peloton.SimRunner` are the executable surfaces. A later Godot client must use Application Commands and knowledge-bounded Queries rather than owning World State or opening SQLite directly.
