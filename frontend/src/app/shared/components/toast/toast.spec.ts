import { ComponentFixture, TestBed } from '@angular/core/testing';
import { jest } from '@jest/globals';

import { Toast } from './toast';
import { ToastService, ToastType } from '../../../core/services/Toast/toast';

describe('Toast', () => {
    let component: Toast;
    let fixture: ComponentFixture<Toast>;
    let toastService: {
        toasts: jest.Mock;
        remove: jest.Mock;
    };

    beforeEach(async () => {
        toastService = {
            toasts: jest.fn().mockReturnValue([]),
            remove: jest.fn(),
        };

        await TestBed.configureTestingModule({
            imports: [Toast],
            providers: [
                {
                    provide: ToastService,
                    useValue: toastService,
                },
            ],
        }).compileComponents();

        fixture = TestBed.createComponent(Toast);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('deve criar o componente', () => {
        expect(component).toBeTruthy();
    });

    it.each([
        ['success', '✓'],
        ['error', '✕'],
        ['warning', '!'],
        ['info', 'i'],
    ] as [ToastType, string][])(
        'deve retornar o ícone correto para o tipo %s',
        (type, expectedIcon) => {
            expect(component.iconFor(type)).toBe(expectedIcon);
        },
    );

    it('deve remover uma notificação pelo ID', () => {
        component.dismiss(123);

        expect(toastService.remove).toHaveBeenCalledTimes(1);
        expect(toastService.remove).toHaveBeenCalledWith(123);
    });

    it('deve remover notificações com IDs diferentes', () => {
        component.dismiss(1);
        component.dismiss(2);

        expect(toastService.remove).toHaveBeenNthCalledWith(1, 1);
        expect(toastService.remove).toHaveBeenNthCalledWith(2, 2);
        expect(toastService.remove).toHaveBeenCalledTimes(2);
    });
});