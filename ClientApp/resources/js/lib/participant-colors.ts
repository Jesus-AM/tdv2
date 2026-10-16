// El índice estable y la exclusión entre participantes activos los arbitra el servidor.
// Paleta de identidad independiente del azul de tablas y de los estados rojo/ámbar/verde.
export const participantColors = ['#713C91', '#006A73', '#A03D75', '#765448', '#51575D', '#574198', '#007C91', '#815472'];
export const participantColor = (index = 0) => participantColors[index % participantColors.length] || participantColors[0];
