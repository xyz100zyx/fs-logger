const fs = require('fs');
const path = require('path');

const filePath = path.join(__dirname, '/watched/test_for');

fs.writeFileSync(filePath, '');

const stream = fs.createWriteStream(filePath, { flags: 'a' });

const interval = setInterval(() => {
    const line = `[${new Date().toISOString()}]\n`;
    stream.write(line);
}, 100);

process.on('SIGINT', () => {
    clearInterval(interval);
    stream.end(() => {
        process.exit(0);
    });
});