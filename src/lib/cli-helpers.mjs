export function parseArgs(argv) {
    const [command = "help", ...rest] = argv;
    const options = {};

    for (let index = 0; index < rest.length; index += 1) {
        const token = rest[index];
        if (!token.startsWith("--")) {
            continue;
        }

        const key = token.slice(2).replace(/-([a-z])/g, (_, char) => char.toUpperCase());
        const next = rest[index + 1];
        if (next && !next.startsWith("--")) {
            options[key] = next;
            index += 1;
        } else {
            options[key] = true;
        }
    }

    return { command, options };
}

export function formatOutput(value) {
    console.log(JSON.stringify(value, null, 2));
}
