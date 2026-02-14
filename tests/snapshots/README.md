# Snapshot Tests

This directory is reserved for Verify.Xunit snapshot files.

Snapshot tests are located in the integration test projects:
- `src/controlplane/Icarus.Orchestrator.Tests.Integration/SnapshotTests.cs`

## How Snapshots Work

1. On first run, Verify creates `.verified.txt` files with the expected output
2. On subsequent runs, it compares actual output against the verified snapshot
3. If output changes, a `.received.txt` file is created for review

## Updating Snapshots

To accept new snapshots after intentional changes:
```bash
# Review the diff
diff *.received.txt *.verified.txt

# Accept the new snapshot
mv *.received.txt *.verified.txt
```

Or use the Verify tooling for your IDE.
