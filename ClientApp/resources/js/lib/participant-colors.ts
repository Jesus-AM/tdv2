// El índice lo arbitra PostgreSQL al reservar; el navegador sólo aplica esta paleta sin rojo.
export const participantColors = ['#2459a6', '#6d469b', '#08756b', '#8a5a08', '#33658a', '#687016', '#4e587f', '#835028'];
export const participantColor = (index = 0) => participantColors[index % participantColors.length] || participantColors[0];
