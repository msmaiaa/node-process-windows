const fs = require('fs');
const unzip = require('unzipper');
const fetch = require('node-fetch');
const moveFile = require('move-file');
const downloadDetails = require('./downloadDetails.json');

async function downloadFile(url, path) {
    const res = await fetch(url);
    const fileStream = fs.createWriteStream(path);

    await new Promise((resolve, reject) => {
        res.body.pipe(fileStream);
        res.body.on("error", (err) => {
            reject(err);
        });

        fileStream.on("finish", () => {
            resolve();
        });
    });
}

downloadFile(downloadDetails.url, downloadDetails.zipPath).then(() => {
    console.log('archive download was successful');

    const readStream = fs.createReadStream(downloadDetails.zipPath)
        .pipe(unzip.Extract({
            path: downloadDetails.extractPath
        }));

    readStream.on('close', () => {
        fs.unlink('./archive', (err ) => {
            err ? console.error(err.message) : console.log('file deleted');
        });

        fs.unlink(downloadDetails.zipPath, (err ) => {
            err ? console.error(err.message) : console.log('file deleted');
        });

        (async () => {
            await moveFile(`${downloadDetails.extractPath}/archive`, './archive').catch((err) => {
                console.error(err.message);
            });

            console.log('file has been moved');

            fs.unlink(downloadDetails.extractPath, (err ) => {
                err ? console.error(err.message) : console.log('file deleted');
            });
        })();
    });
});