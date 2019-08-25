import CryptoJs from './crypto-js';

class Encryption {
    isEmpty = (text) => {
        return text === undefined || text === null || text === '';
    }

    rsaEncrypt = (plainText, key) => {
        return new Promise((resolve, reject) => {
            if (isEmpty(plainText)) {
                resolve('');
            }
            try {
                const encrypt = new JSEncrypt();
                encrypt.setPublicKey(key);
                resolve(encrypt.encrypt(text));
            } catch (e) {
                console.log('Error when encrypt by RSA method.');
                console.log(e);
                reject(e);
            }
        });
    }



}