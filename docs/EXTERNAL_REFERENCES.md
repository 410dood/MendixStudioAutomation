# External References Review

This document records what we reviewed from external Mendix/MCP references and what we adopted into this repository.

## Sources reviewed

- npm package: `@jordnlvr/mendix-mcp-server`
  - https://www.npmjs.com/package/@jordnlvr/mendix-mcp-server
- gist:
  - https://gist.github.com/ruvnet/7b6843c457822cbcf42fc4aa635eadbb

## What was useful for this repo

From `@jordnlvr/mendix-mcp-server`, the most directly useful pattern for our Studio Pro automation goals was explicit **knowledge gap tracking**:

- Capture missing/fragile capabilities as structured records.
- Keep a local backlog that can be summarized and prioritized.
- Make hardening work data-driven instead of ad hoc.

Adopted in this repo:

- New local knowledge-gap store: `.automation-state/knowledge-gaps.json`
- New CLI commands:
  - `record-knowledge-gap`
  - `list-knowledge-gaps`
  - `summarize-knowledge-gaps`
- New operation catalog entries:
  - `knowledge.recordGap`
  - `knowledge.listGaps`
  - `knowledge.summarizeGaps`

## What was not adopted

The reviewed gist is primarily about ChatGPT Apps custom UI (`window.openai` bridge, iframe rendering, event loop). That is useful for App SDK products, but not directly relevant to this repo’s end goal of Studio Pro desktop automation and extension-backed Mendix model mutation.

No direct implementation from that gist was added to core automation code.
