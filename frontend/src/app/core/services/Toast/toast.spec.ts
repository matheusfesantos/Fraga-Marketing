import { TestBed } from '@angular/core/testing';
import { ToastService, ToastType } from './toast';
import { jest } from '@jest/globals';

describe('ToastService', () => {
    let service: ToastService;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [ToastService],
        });

        service = TestBed.inject(ToastService);
    });

    afterEach(() => {
        jest.useRealTimers();
    });

    it('deve ser criado', () => {
        expect(service).toBeTruthy();
    });

    it('deve iniciar sem notificações', () => {
        expect(service.toasts()).toEqual([]);
    });

    it('deve adicionar uma notificação de sucesso', () => {
        service.success('Operação realizada');

        expect(service.toasts()).toHaveLength(1);
        expect(service.toasts()[0]).toEqual({
            id: 1,
            type: 'success',
            title: 'Sucesso',
            message: 'Operação realizada',
        });
    });

    it('deve permitir personalizar o título da notificação de sucesso', () => {
        service.success('Conta criada', 'Tudo certo');

        expect(service.toasts()[0].title).toBe('Tudo certo');
        expect(service.toasts()[0].message).toBe('Conta criada');
    });

    it('deve adicionar uma notificação de erro', () => {
        service.error('Falha ao salvar');

        expect(service.toasts()[0]).toEqual({
            id: 1,
            type: 'error',
            title: 'Erro',
            message: 'Falha ao salvar',
        });
    });

    it('deve permitir personalizar o título da notificação de erro', () => {
        service.error('Falha na API', 'Erro de conexão');

        expect(service.toasts()[0].title).toBe('Erro de conexão');
    });

    it('deve adicionar uma notificação de alerta', () => {
        service.warning('Confira os dados');

        expect(service.toasts()[0]).toEqual({
            id: 1,
            type: 'warning',
            title: 'Atenção',
            message: 'Confira os dados',
        });
    });

    it('deve permitir personalizar o título da notificação de alerta', () => {
        service.warning('Saldo baixo', 'Atenção ao saldo');

        expect(service.toasts()[0].title).toBe('Atenção ao saldo');
    });

    it('deve adicionar uma notificação informativa', () => {
        service.info('Nova atualização disponível');

        expect(service.toasts()[0]).toEqual({
            id: 1,
            type: 'info',
            title: 'Informação',
            message: 'Nova atualização disponível',
        });
    });

    it('deve permitir personalizar o título da notificação informativa', () => {
        service.info('Atualização disponível', 'Novidade');

        expect(service.toasts()[0].title).toBe('Novidade');
    });

    it('deve gerar identificadores diferentes para notificações', () => {
        service.success('Primeira');
        service.error('Segunda');
        service.info('Terceira');

        const toasts = service.toasts();

        expect(toasts).toHaveLength(3);
        expect(toasts.map((toast) => toast.id)).toEqual([1, 2, 3]);
    });

    it('deve manter as notificações anteriores ao adicionar uma nova', () => {
        service.success('Primeira');
        service.info('Segunda');

        expect(service.toasts()).toHaveLength(2);
        expect(service.toasts()[0].message).toBe('Primeira');
        expect(service.toasts()[1].message).toBe('Segunda');
    });

    it('deve remover uma notificação pelo identificador', () => {
        service.success('Primeira');
        service.error('Segunda');

        service.remove(1);

        expect(service.toasts()).toHaveLength(1);
        expect(service.toasts()[0].id).toBe(2);
        expect(service.toasts()[0].message).toBe('Segunda');
    });

    it('não deve alterar as notificações ao remover um identificador inexistente', () => {
        service.success('Notificação existente');

        service.remove(999);

        expect(service.toasts()).toHaveLength(1);
        expect(service.toasts()[0].message).toBe('Notificação existente');
    });

    it('deve remover automaticamente a notificação após quatro segundos', () => {
        jest.useFakeTimers();

        service.success('Notificação temporária');

        expect(service.toasts()).toHaveLength(1);

        jest.advanceTimersByTime(3999);

        expect(service.toasts()).toHaveLength(1);

        jest.advanceTimersByTime(1);

        expect(service.toasts()).toHaveLength(0);
    });

    it('deve remover automaticamente apenas a notificação correspondente', () => {
        jest.useFakeTimers();

        service.success('Primeira');

        jest.advanceTimersByTime(2000);

        service.info('Segunda');

        jest.advanceTimersByTime(2000);

        expect(service.toasts()).toHaveLength(1);
        expect(service.toasts()[0].message).toBe('Segunda');

        jest.advanceTimersByTime(2000);

        expect(service.toasts()).toHaveLength(0);
    });

    it('deve continuar gerando identificadores sequenciais após remover notificações', () => {
        service.success('Primeira');
        service.remove(1);
        service.warning('Segunda');

        expect(service.toasts()).toHaveLength(1);
        expect(service.toasts()[0].id).toBe(2);
    });

    it('deve utilizar somente os tipos de notificação permitidos', () => {
        const cases: {
            type: ToastType;
            show: () => void;
        }[] = [
                {
                    type: 'success',
                    show: () => service.success('Mensagem'),
                },
                {
                    type: 'error',
                    show: () => service.error('Mensagem'),
                },
                {
                    type: 'warning',
                    show: () => service.warning('Mensagem'),
                },
                {
                    type: 'info',
                    show: () => service.info('Mensagem'),
                },
            ];

        cases.forEach(({ type, show }) => {
            show();

            const toast = service.toasts().find(
                (item) => item.type === type && item.message === 'Mensagem',
            );

            expect(toast).toBeDefined();
            expect(toast?.type).toBe(type);
        });
    });
});